import { Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { QRCodeComponent } from 'angularx-qrcode';
import { AttendanceSessionsApi } from '../../../../core/api/attendance-sessions-api';

/**
 * Panneau **QR rotatif** (feature 014, US1). Récupère le jeton courant via l'API et en **génère
 * l'image** côté client (bibliothèque QR). Le jeton est **ré-interrogé et regénéré avant expiration**
 * (au rythme `stepSeconds`). Le cycle est **borné au cycle de vie** du composant (arrêté à la
 * destruction — donc à la clôture, le parent retirant le panneau). Le **jeton n'est jamais affiché en
 * clair ni persisté** (FR-005/SC-005) : il n'alimente que le rendu de l'image.
 */
@Component({
  selector: 'app-qr-panel',
  imports: [QRCodeComponent],
  template: `
    <div class="lx-qr" style="text-align:center;">
      @if (qrData()) {
        <qrcode [qrdata]="qrData()!" [width]="320" errorCorrectionLevel="M" />
        <p class="lx-muted">Faites scanner ce code pour enregistrer une présence.</p>
      } @else if (errorState()) {
        <p class="lx-muted">Rafraîchissement du code en cours…</p>
      } @else {
        <p class="lx-muted">Génération du code…</p>
      }
    </div>
  `,
})
export class QrPanelComponent implements OnInit, OnDestroy {
  private readonly sessionsApi = inject(AttendanceSessionsApi);

  /** Identifiant de la session dont on projette le QR. */
  readonly sessionId = input.required<number>();

  /** Donnée servant à générer l'image (jeton courant). Jamais rendue en texte. */
  readonly qrData = signal<string | null>(null);
  readonly errorState = signal(false);

  private timer: ReturnType<typeof setTimeout> | null = null;
  private destroyed = false;

  ngOnInit(): void {
    this.fetch();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.stopTimer();
  }

  private fetch(): void {
    this.sessionsApi.qr(this.sessionId()).subscribe({
      next: (res) => {
        // Charge versionnée consommée par l'app mobile (feature 026) :
        // { v, s (sessionId), t (jeton) }. Le jeton n'est jamais rendu en texte.
        this.qrData.set(JSON.stringify({ v: 1, s: this.sessionId(), t: res.token }));
        this.errorState.set(false);
        // Regénérer AVANT expiration : petite marge sur le pas de rotation.
        const delayMs = Math.max((res.stepSeconds - 1) * 1000, 1000);
        this.scheduleNext(delayMs);
      },
      error: (_err: HttpErrorResponse) => {
        // État transitoire : on réessaie sans planter ni figer un QR périmé.
        this.errorState.set(true);
        this.scheduleNext(2000);
      },
    });
  }

  private scheduleNext(delayMs: number): void {
    this.stopTimer();
    if (this.destroyed) {
      return;
    }
    this.timer = setTimeout(() => this.fetch(), delayMs);
  }

  private stopTimer(): void {
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }
}
