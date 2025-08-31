import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { forkJoin, of, tap } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { AppointmentService } from '../services/appointment.service';
import { MedicoService } from '../services/medico.service';
import { PatientService, Paciente } from '../services/patient.service';
import { Medico } from '../interfaces/medico';

type ConsultaCard = {
  dataHora: Date;
  pacienteId: string;
  pacienteNome?: string;
};

type SchedulleDto = { patientId: string; day: string; hour: string };
type DoctorConsultasDto = {
  id: string;
  name: string;
  speciality: string;
  schedulles: SchedulleDto[];
};

@Component({
  selector: 'app-consultas',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Minhas Consultas</mat-card-title>
        <mat-card-subtitle>
          {{ medico.speciality || '—' }} • {{ medico.name || 'Médico' }}
        </mat-card-subtitle>
      </mat-card-header>

      <!-- Admin escolhe o médico -->
      <mat-card-content *ngIf="auth.isAdmin">
        <mat-form-field appearance="outline" style="min-width: 320px;">
          <mat-label>Selecione o médico</mat-label>
          <mat-select [formControl]="doctorCtrl"
                      (selectionChange)="onDoctorChange($event.value)">
            <mat-option *ngFor="let m of medicos; trackBy: trackByMed" [value]="m.id">
              {{ m.name }} — {{ m.speciality }}
            </mat-option>
          </mat-select>
        </mat-form-field>
      </mat-card-content>
    </mat-card>

    <section class="list">
      <mat-card class="appt" *ngFor="let c of consultas; trackBy: trackByConsulta">
        <mat-card-title>
          Paciente: {{ c.pacienteNome || (c.pacienteId | slice:0:8) + '…' }}
        </mat-card-title>
        <mat-card-subtitle>
          {{ c.dataHora | date:'fullDate' }} — {{ c.dataHora | date:'HH:mm' }}
        </mat-card-subtitle>
      </mat-card>

      <p *ngIf="consultas.length === 0" class="empty-hint">
        Nenhuma consulta marcada para esse médico.
      </p>
    </section>
  `,
  styles: [`
    :host { display:block; width:100%; }
    mat-card { margin:1rem; padding:1rem; }
    .list { display:grid; grid-template-columns:repeat(auto-fill, minmax(260px,1fr)); gap:1rem; padding:1rem; }
    .appt { border-left:4px solid #e91e63; }
    .empty-hint { opacity:.7; }
  `]
})
export class ConsultaComponent implements OnInit {
  // header mostrado
  medico = { name: '', speciality: '' };

  // admin -> select de médico (reactive)
  doctorCtrl = new FormControl<string | null>(null);
  medicos: Medico[] = [];

  // cards
  consultas: ConsultaCard[] = [];

  // cache p/ nomes dos pacientes
  private patientCache = new Map<string, Paciente>();
  private norm = (id: string) => (id ?? '').trim().toLowerCase();

  constructor(
    public auth: AuthService,
    private apptService: AppointmentService,
    private medicoService: MedicoService,
    private patientService: PatientService
  ) { }

  ngOnInit(): void {
    if (this.auth.isAdmin) {
      // Admin: carrega lista de médicos; só busca consultas ao selecionar
      this.medicoService.getAllAsync().subscribe({
        next: list => {
          this.medicos = list;
          // opcional: selecionar o primeiro automaticamente
          // if (this.medicos.length) this.doctorCtrl.setValue(this.medicos[0].id);
        },
        error: err => console.error('Erro ao carregar médicos', err)
      });
      return;
    }

    // Médico: usa o id do token
    const healthcareId = this.auth.userId;
    if (!healthcareId) return;

    this.loadConsultas(healthcareId);
  }

  onDoctorChange(healthcareId: string | null) {
    this.consultas = [];
    if (healthcareId) this.loadConsultas(healthcareId);
  }

  private loadConsultas(healthcareId: string) {
    this.apptService.getByDoctor(healthcareId).subscribe({
      next: (res: DoctorConsultasDto) => {
        // header
        this.medico = { name: res.name, speciality: res.speciality };

        const schedules = res.schedulles ?? [];
        if (schedules.length === 0) {
          this.consultas = [];
          return;
        }

        const uniquePatientIds = Array.from(new Set(schedules.map(s => s.patientId)));

        // carrega nomes de pacientes necessários
        forkJoin(uniquePatientIds.map(id => this.fetchPatient(id))).subscribe({
          next: () => {
            this.consultas = schedules
              .slice()
              .sort((a, b) => a.hour.localeCompare(b.hour))
              .map(s => {
                const p = this.patientCache.get(this.norm(s.patientId));
                return {
                  dataHora: new Date(s.hour),
                  pacienteId: s.patientId,
                  pacienteNome: p?.name
                };
              });
          },
          error: () => {
            // fallback sem nomes
            this.consultas = schedules
              .slice()
              .sort((a, b) => a.hour.localeCompare(b.hour))
              .map(s => ({
                dataHora: new Date(s.hour),
                pacienteId: s.patientId
              }));
          }
        });
      },
      error: err => console.error('Erro ao carregar consultas', err)
    });
  }

  private fetchPatient(id: string) {
    const key = this.norm(id);
    const hit = this.patientCache.get(key);
    if (hit) return of(hit);
    return this.patientService.getById(id).pipe(tap(p => this.patientCache.set(key, p)));
  }

  // trackBys
  trackByMed = (_: number, m: Medico) => m.id;
  trackByConsulta = (_: number, c: ConsultaCard) => c.pacienteId + '|' + c.dataHora.toISOString();
}
