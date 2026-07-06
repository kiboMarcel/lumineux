import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AntennasApi } from '../../../core/api/antennas-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { ProblemDetails } from '../../../core/api/models';
import { messageForError } from '../../../core/http/error-messages';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { AntennaResponse } from '../antenna.models';

/**
 * Liste de gestion des antennes (feature 017, US1/US4) : affiche **toutes** les antennes (actives et
 * inactives) avec leur statut, et permet de créer, modifier, désactiver (confirmé) / réactiver.
 * L'accès est gardé par `permissionGuard('manage_referentials')` ; l'API reste l'autorité.
 */
@Component({
  selector: 'app-antenna-list',
  imports: [RouterLink],
  template: `
    <div class="lx-card">
      <div class="lx-topbar" style="border:0; padding:0; margin-bottom:1rem;">
        <h1 class="lx-title" style="margin:0;">Antennes</h1>
        <a class="lx-btn" routerLink="/antennas/new">Nouvelle antenne</a>
      </div>

      @if (loading()) {
        <p class="lx-muted">Chargement…</p>
      } @else if (antennas().length === 0) {
        <p class="lx-muted">Aucune antenne. Créez-en une pour commencer.</p>
      } @else {
        <div style="overflow-x:auto;">
          <table style="width:100%; border-collapse:collapse;">
            <thead>
              <tr>
                <th style="text-align:left;">Code</th>
                <th style="text-align:left;">Libellé</th>
                <th style="text-align:left;">District</th>
                <th style="text-align:left;">Statut</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (a of antennas(); track a.id) {
                <tr>
                  <td>{{ a.code }}</td>
                  <td>{{ a.label }}</td>
                  <td>{{ districtLabel(a.districtId) }}</td>
                  <td>{{ a.status === 'Active' ? 'Active' : 'Inactive' }}</td>
                  <td style="white-space:nowrap;">
                    <a class="lx-btn lx-btn-ghost" [routerLink]="['/antennas', a.id, 'edit']">Modifier</a>
                    @if (a.status === 'Active') {
                      <button type="button" class="lx-btn lx-btn-ghost" [disabled]="busyId() === a.id" (click)="deactivate(a)">Désactiver</button>
                    } @else {
                      <button type="button" class="lx-btn lx-btn-ghost" [disabled]="busyId() === a.id" (click)="activate(a)">Réactiver</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class AntennaListComponent {
  private readonly api = inject(AntennasApi);
  private readonly referenceApi = inject(ReferenceApi);
  private readonly notifier = inject(NotificationService);

  readonly antennas = signal<AntennaResponse[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<number | null>(null);
  private readonly districts = signal<Map<number, string>>(new Map());

  constructor() {
    this.load();
  }

  districtLabel(id: number): string {
    return this.districts().get(id) ?? `#${id}`;
  }

  private load(): void {
    this.loading.set(true);
    forkJoin({ antennas: this.api.list(), districts: this.referenceApi.districts() }).subscribe({
      next: (r) => {
        this.antennas.set(r.antennas);
        this.districts.set(new Map(r.districts.map((d) => [d.id, d.label])));
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.notifier.error(messageForError(err));
        this.loading.set(false);
      },
    });
  }

  deactivate(antenna: AntennaResponse): void {
    if (!confirm(`Désactiver l'antenne « ${antenna.code} » ? Elle ne sera plus proposée à la sélection.`)) {
      return;
    }
    this.busyId.set(antenna.id);
    this.api.deactivate(antenna.id).subscribe({
      next: () => { this.busyId.set(null); this.load(); },
      error: (err: HttpErrorResponse) => { this.busyId.set(null); this.notifier.error(this.statusError(err)); },
    });
  }

  activate(antenna: AntennaResponse): void {
    this.busyId.set(antenna.id);
    this.api.activate(antenna.id).subscribe({
      next: () => { this.busyId.set(null); this.load(); },
      error: (err: HttpErrorResponse) => { this.busyId.set(null); this.notifier.error(this.statusError(err)); },
    });
  }

  /** Libellé dédié pour le refus « session ouverte », sinon mapping générique. */
  private statusError(err: HttpErrorResponse): string {
    const code = (err.error as ProblemDetails | null)?.code;
    if (err.status === 409 && code === 'antenna_has_open_sessions') {
      return 'Impossible de désactiver cette antenne : une session de présence est encore ouverte.';
    }
    return messageForError(err);
  }
}
