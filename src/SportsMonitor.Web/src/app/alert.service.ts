import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { Divergence, VerificationUpdate } from './models';

@Injectable({ providedIn: 'root' })
export class AlertService {
  readonly divergences = signal<Divergence[]>([]);
  readonly connected = signal(false);

  private hub = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/alerts')
    .withAutomaticReconnect()
    .build();

  constructor(private http: HttpClient) {}

  async init(): Promise<void> {
    const recent = await this.http.get<Divergence[]>('/api/divergences?limit=50').toPromise();
    this.divergences.set(recent ?? []);

    this.hub.on('ReceiveAlert', (d: Divergence) => {
      this.divergences.update(list => [d, ...list]);
      this.playAlert();
    });

    this.hub.onreconnected(() => this.connected.set(true));
    this.hub.onclose(() => this.connected.set(false));

    await this.hub.start();
    this.connected.set(true);
  }

  async verify(id: string, update: VerificationUpdate): Promise<void> {
    await this.http.post(`/api/divergences/${id}/verify`, update).toPromise();
    this.divergences.update(list =>
      list.map(d => d.id === id ? { ...d, ...update, verificationStatus: update.status } : d)
    );
  }

  private playAlert(): void {
    const ctx = new AudioContext();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.type = 'sine';
    osc.frequency.setValueAtTime(880, ctx.currentTime);
    osc.frequency.exponentialRampToValueAtTime(1760, ctx.currentTime + 0.08);
    osc.frequency.exponentialRampToValueAtTime(880, ctx.currentTime + 0.16);
    gain.gain.setValueAtTime(0.35, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.25);
    osc.start();
    osc.stop(ctx.currentTime + 0.25);
  }
}
