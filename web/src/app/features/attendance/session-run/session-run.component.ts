import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { AttendanceSessionsApi } from '../../../core/api/attendance-sessions-api';
import { AttendancesApi } from '../../../core/api/attendances-api';
import { messageForError } from '../../../core/http/error-messages';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { AttendanceResponse, AttendanceStatusFilter, SessionResponse } from '../attendance.models';
import { QrPanelComponent } from './qr-panel/qr-panel.component';
import { ManualAddComponent } from './manual-add/manual-add.component';

/** Intervalle de rafraîchissement de la liste des présences (temps réel par polling). */
const POLL_INTERVAL_MS = 5000;

/**
 * Écran d'**animation** d'une session (feature 014, US1–US4). Charge la session par identifiant,
 * projette le **QR rotatif** (US1), **suit les présences en temps réel** par rafraîchissement
 * périodique (US2), permet l'**ajout manuel** et l'**annulation** (US3) et la **clôture** (US4).
 * Après clôture, le QR et les actions d'écriture ne sont plus proposés (FR-011). Les cycles de
 * rafraîchissement sont bornés au cycle de vie du composant et arrêtés à la clôture.
 */
@Component({
  selector: 'app-session-run',
  imports: [QrPanelComponent, ManualAddComponent],
  template: `
    <div class="lx-card">
      @if (loading()) {
        <p class="lx-muted">Chargement de la session…</p>
      } @else if (session(); as s) {
        <div class="lx-topbar" style="border:0; padding:0; margin-bottom:1rem;">
          <h1 class="lx-title" style="margin:0;">Session de présence #{{ s.id }}</h1>
          <span class="lx-muted">{{ s.meetingDate }} — {{ isClosed() ? 'Clôturée' : 'Ouverte' }}</span>
        </div>

        @if (isClosed()) {
          <p class="lx-muted">Session clôturée@if (s.endTime) { le {{ s.endTime }}}. Les présences sont figées.</p>
        } @else {
          <button type="button" class="lx-btn lx-btn-ghost" (click)="close()" [disabled]="closing()">
            {{ closing() ? 'Clôture…' : 'Clôturer la session' }}
          </button>
        }
      } @else {
        <p class="lx-error">{{ error() || 'Session introuvable.' }}</p>
      }
    </div>

    @if (session() && !isClosed()) {
      <div class="lx-card" style="margin-top:1rem;">
        <app-qr-panel [sessionId]="session()!.id" />
      </div>
    }

    @if (session()) {
      <div class="lx-card" style="margin-top:1rem;">
        <div class="lx-topbar" style="border:0; padding:0; margin-bottom:0.75rem;">
          <h2 style="margin:0; font-size:1.1rem;">Présences — {{ validCount() }} valide(s)</h2>
          <div class="lx-links" style="margin:0;">
            @for (f of filters; track f.value) {
              <button type="button" class="lx-btn lx-btn-ghost"
                      [style.font-weight]="filter() === f.value ? '700' : '400'"
                      (click)="changeFilter(f.value)">{{ f.label }}</button>
            }
          </div>
        </div>

        @if (attendances().length === 0) {
          <p class="lx-muted">Aucune présence pour ce filtre.</p>
        } @else {
          <div style="overflow-x:auto;">
            <table style="width:100%; border-collapse:collapse;">
              <thead>
                <tr>
                  <th style="text-align:left;">Membre</th>
                  <th style="text-align:left;">Arrivée</th>
                  <th style="text-align:left;">Source</th>
                  <th style="text-align:left;">Statut</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                @for (a of attendances(); track a.id) {
                  <tr>
                    <td>{{ a.memberFullName || ('#' + a.memberId) }}</td>
                    <td>{{ a.arrivalTime }}</td>
                    <td>{{ a.source }}</td>
                    <td>{{ a.status }}</td>
                    <td>
                      @if (!isClosed() && a.status !== 'Cancelled') {
                        <button type="button" class="lx-btn lx-btn-ghost" (click)="cancel(a)">Annuler</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>

      @if (!isClosed()) {
        <app-manual-add [sessionId]="session()!.id" (added)="loadAttendances()" />
      }
    }
  `,
  styles: [`.lx-error { color: var(--lx-danger, #c0392b); }`],
})
export class SessionRunComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly sessionsApi = inject(AttendanceSessionsApi);
  private readonly attendancesApi = inject(AttendancesApi);
  private readonly notifier = inject(NotificationService);

  readonly filters: { value: AttendanceStatusFilter; label: string }[] = [
    { value: 'Valid', label: 'Valides' },
    { value: 'Cancelled', label: 'Annulées' },
    { value: 'All', label: 'Toutes' },
  ];

  readonly session = signal<SessionResponse | null>(null);
  readonly attendances = signal<AttendanceResponse[]>([]);
  readonly validCount = signal(0);
  readonly filter = signal<AttendanceStatusFilter>('Valid');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly closing = signal(false);

  readonly isClosed = computed(() => this.session()?.status === 'Closed');

  private sessionId = 0;
  private pollTimer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.sessionId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadSession();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private loadSession(): void {
    this.sessionsApi.get(this.sessionId).subscribe({
      next: (s) => {
        this.session.set(s);
        this.loading.set(false);
        this.loadAttendances();
        if (s.status !== 'Closed') {
          this.startPolling();
        }
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(messageForError(err));
        this.loading.set(false);
      },
    });
  }

  loadAttendances(): void {
    this.attendancesApi.list(this.sessionId, this.filter()).subscribe({
      next: (res) => {
        this.attendances.set(res.items);
        this.validCount.set(res.validCount);
      },
      error: () => {
        // Erreur transitoire de rafraîchissement : on n'interrompt pas la séance.
      },
    });
  }

  changeFilter(value: AttendanceStatusFilter): void {
    this.filter.set(value);
    this.loadAttendances();
  }

  cancel(attendance: AttendanceResponse): void {
    const who = attendance.memberFullName || `#${attendance.memberId}`;
    if (!confirm(`Annuler la présence de ${who} ?`)) {
      return;
    }
    this.attendancesApi.cancel(this.sessionId, attendance.memberId).subscribe({
      next: () => this.loadAttendances(),
      error: (err: HttpErrorResponse) => this.notifier.error(messageForError(err)),
    });
  }

  close(): void {
    if (this.closing() || !confirm('Clôturer définitivement la session ? Aucune présence ne pourra plus être modifiée.')) {
      return;
    }
    this.closing.set(true);
    this.sessionsApi.close(this.sessionId).subscribe({
      next: (s) => {
        this.session.set(s);
        this.closing.set(false);
        this.stopPolling();
        this.loadAttendances();
      },
      error: (err: HttpErrorResponse) => {
        this.notifier.error(messageForError(err));
        this.closing.set(false);
      },
    });
  }

  private startPolling(): void {
    this.stopPolling();
    this.pollTimer = setInterval(() => this.loadAttendances(), POLL_INTERVAL_MS);
  }

  private stopPolling(): void {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
