import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ReferenceApi } from '../../../core/api/reference-api';
import { ReferenceItem } from '../../../core/api/reference.models';
import { AttendanceSessionsApi } from '../../../core/api/attendance-sessions-api';
import { messageForError } from '../../../core/http/error-messages';

/**
 * Écran de **démarrage** d'une session de présence (feature 014, US1). Sélection d'une antenne
 * (référentiel 010), d'une date de réunion et d'un pas de rotation du QR optionnel, puis navigation
 * vers l'écran d'animation. Si aucune antenne n'est disponible, le démarrage est **empêché** (FR-002).
 */
@Component({
  selector: 'app-session-start',
  imports: [FormsModule],
  template: `
    <div class="lx-card">
      <h1 class="lx-title" style="margin-top:0;">Démarrer une session de présence</h1>

      @if (loadingRefs()) {
        <p class="lx-muted">Chargement des antennes…</p>
      } @else if (antennas().length === 0) {
        <p class="lx-error">Aucune antenne active n'est disponible : impossible de démarrer une session.</p>
      } @else {
        <form (ngSubmit)="start()" style="display:flex; flex-direction:column; gap:1rem; max-width:420px;">
          <label style="display:flex; flex-direction:column; gap:0.25rem;">
            <span>Antenne</span>
            <select [(ngModel)]="antennaId" name="antennaId" required
                    style="padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;">
              <option [ngValue]="null" disabled>— Choisir une antenne —</option>
              @for (a of antennas(); track a.id) {
                <option [ngValue]="a.id">{{ a.label }}</option>
              }
            </select>
          </label>

          <label style="display:flex; flex-direction:column; gap:0.25rem;">
            <span>Date de réunion</span>
            <input type="date" [(ngModel)]="meetingDate" name="meetingDate" required
                   style="padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;" />
          </label>

          <label style="display:flex; flex-direction:column; gap:0.25rem;">
            <span>Pas de rotation du QR (secondes, optionnel)</span>
            <input type="number" min="5" [(ngModel)]="qrStepSeconds" name="qrStepSeconds"
                   placeholder="Valeur par défaut du serveur"
                   style="padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;" />
          </label>

          @if (error()) {
            <p class="lx-error">{{ error() }}</p>
          }

          <button type="submit" class="lx-btn" [disabled]="submitting() || antennaId === null || !meetingDate">
            {{ submitting() ? 'Démarrage…' : 'Démarrer la session' }}
          </button>
        </form>
      }
    </div>
  `,
  styles: [`.lx-error { color: var(--lx-danger, #c0392b); }`],
})
export class SessionStartComponent {
  private readonly refApi = inject(ReferenceApi);
  private readonly sessionsApi = inject(AttendanceSessionsApi);
  private readonly router = inject(Router);

  readonly antennas = signal<ReferenceItem[]>([]);
  readonly loadingRefs = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  antennaId: number | null = null;
  meetingDate = '';
  qrStepSeconds: number | null = null;

  constructor() {
    this.refApi.antennas().subscribe({
      next: (list) => {
        this.antennas.set(list);
        this.loadingRefs.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(messageForError(err));
        this.loadingRefs.set(false);
      },
    });
  }

  start(): void {
    if (this.antennaId === null || !this.meetingDate || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.sessionsApi
      .start({
        antennaId: this.antennaId,
        meetingDate: this.meetingDate,
        qrStepSeconds: this.qrStepSeconds ?? null,
      })
      .subscribe({
        next: (session) => {
          void this.router.navigate(['/attendance/sessions', session.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(messageForError(err));
          this.submitting.set(false);
        },
      });
  }
}
