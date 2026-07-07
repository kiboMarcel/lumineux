import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ReferenceApi } from '../../../core/api/reference-api';
import { ReferenceItem } from '../../../core/api/reference.models';
import { AttendanceSessionsApi } from '../../../core/api/attendance-sessions-api';
import { messageForError } from '../../../core/http/error-messages';
import { SessionResponse } from '../attendance.models';

/**
 * Écran de **démarrage** d'une session de présence (feature 014, US1) + **reprise** d'une session en
 * cours (feature 024). Au chargement, récupère les sessions ouvertes de l'utilisateur (API 023) et
 * propose de les **reprendre** ; en cas de **conflit** au démarrage (409), propose la reprise de la
 * session correspondante. La liste des sessions ouvertes provient de l'API — aucun filtrage métier
 * côté client. La vérification est **non bloquante** pour le formulaire de démarrage.
 */
@Component({
  selector: 'app-session-start',
  imports: [FormsModule],
  template: `
    <div class="lx-card">
      <h1 class="lx-title" style="margin-top:0;">Démarrer une session de présence</h1>

      <!-- Reprise d'une session en cours (feature 024). -->
      @if (openSessions().length > 0) {
        <div class="lx-alert lx-alert-info" role="status">
          <p style="margin:0 0 0.5rem;"><strong>Vous avez une session en cours.</strong></p>
          <ul style="list-style:none; padding:0; margin:0;">
            @for (s of openSessions(); track s.id) {
              <li style="display:flex; justify-content:space-between; align-items:center; gap:0.75rem; padding:0.35rem 0;">
                <span>{{ antennaLabel(s.antennaId) }} — {{ dayOf(s.meetingDate) }} (démarrée à {{ timeOf(s.startTime) }})</span>
                <button type="button" class="lx-btn lx-btn-ghost" (click)="resume(s)">Reprendre</button>
              </li>
            }
          </ul>
        </div>
      }

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

          <!-- Conflit au démarrage → proposer la reprise (feature 024, US2). -->
          @if (conflictResume(); as c) {
            <div class="lx-alert lx-alert-info" role="alert">
              <p style="margin:0 0 0.5rem;">Une session est déjà ouverte pour cette antenne à ce créneau.</p>
              <button type="button" class="lx-btn" (click)="resume(c)">Reprendre la session en cours</button>
            </div>
          } @else if (error()) {
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

  /** Sessions ouvertes de l'utilisateur (feature 023) + reprise sur conflit (feature 024). */
  readonly openSessions = signal<SessionResponse[]>([]);
  readonly conflictResume = signal<SessionResponse | null>(null);

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

    // Vérification NON bloquante des sessions ouvertes de l'utilisateur (pour la reprise).
    this.sessionsApi.myOpenSessions().subscribe({
      next: (sessions) => this.openSessions.set(sessions),
      error: () => { /* silencieux : ne bloque pas le démarrage d'une nouvelle session */ },
    });
  }

  /** Libellé d'antenne (référentiel 010) — présentation seule. */
  antennaLabel(id: number): string {
    return this.antennas().find((a) => a.id === id)?.label ?? `#${id}`;
  }

  dayOf(meetingDate: string): string {
    return meetingDate.slice(0, 10);
  }

  timeOf(startTime: string): string {
    return startTime.length >= 16 ? startTime.slice(11, 16) : startTime;
  }

  resume(session: SessionResponse): void {
    void this.router.navigate(['/attendance/sessions', session.id]);
  }

  start(): void {
    if (this.antennaId === null || !this.meetingDate || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.conflictResume.set(null);
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
          if (err.status === 409) {
            this.handleStartConflict(err);
          } else {
            this.error.set(messageForError(err));
            this.submitting.set(false);
          }
        },
      });
  }

  /** Conflit au démarrage : retrouver ma session ouverte pour l'antenne + date choisies (feature 024). */
  private handleStartConflict(err: HttpErrorResponse): void {
    this.sessionsApi.myOpenSessions().subscribe({
      next: (sessions) => {
        this.openSessions.set(sessions);
        const match = sessions.find(
          (s) => s.antennaId === this.antennaId && this.dayOf(s.meetingDate) === this.meetingDate,
        );
        this.submitting.set(false);
        if (match) {
          this.conflictResume.set(match);
        } else {
          this.error.set(messageForError(err));
        }
      },
      error: () => {
        this.submitting.set(false);
        this.error.set(messageForError(err));
      },
    });
  }
}
