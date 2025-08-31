import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import { RouterModule } from "@angular/router";
import { FormsModule } from "@angular/forms";

import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatNativeDateModule } from "@angular/material/core";
import { MatButtonModule } from "@angular/material/button";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";

import { switchMap, tap } from "rxjs";

import { AppointmentService } from "../services/appointment.service";
import { Medico } from "../interfaces/medico";
import { MedicoService } from "../services/medico.service";
import { AuthService } from "../services/auth.service";
import { PatientService, Paciente } from "../services/patient.service";

type Appointment = {
  id: string;
  patientId: string;
  date: Date;
  specialty: string;
  doctor: string;
};

@Component({
  selector: "app-agenda",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatTooltipModule,
    MatSnackBarModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Minha Agenda</mat-card-title>
        <mat-card-subtitle>Agendo do paciente</mat-card-subtitle>
      </mat-card-header>

      <mat-card-content class="form-grid">

        <!-- Paciente (só Admin) -->
        <mat-form-field appearance="outline" *ngIf="auth.isAdmin">
          <mat-label>Paciente</mat-label>
          <mat-select [(ngModel)]="selectedPatientId" (selectionChange)="onPatientChange()">
            <mat-option *ngFor="let p of patients; trackBy: trackById" [value]="p.id">
              {{ p.name }} — {{ p.cpf }}
            </mat-option>
          </mat-select>
        </mat-form-field>

        <!-- Especialidade -->
        <mat-form-field appearance="outline">
          <mat-label>Especialidade</mat-label>
          <mat-select [(ngModel)]="selectedSpecialty" (selectionChange)="onSpecialtyChange()">
            <mat-option *ngFor="let esp of especialidades" [value]="esp">{{ esp }}</mat-option>
          </mat-select>
        </mat-form-field>

        <!-- Médico -->
        <mat-form-field appearance="outline">
          <mat-label>Médico</mat-label>
          <mat-select [(ngModel)]="selectedDoctorId" [disabled]="!selectedSpecialty" (selectionChange)="refreshSlots()">
            <mat-option *ngFor="let med of doctorsForSelected()" [value]="med.id">{{ med.name }}</mat-option>
          </mat-select>
        </mat-form-field>

        <!-- Data -->
        <mat-form-field appearance="outline">
          <mat-label>Data de Agendamento</mat-label>
          <input matInput [matDatepicker]="picker"
                 [(ngModel)]="selectedDate"
                 [min]="today"
                 [matDatepickerFilter]="weekdaysFromTodayOnly"
                 (dateChange)="refreshSlots()">
          <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
          <mat-datepicker #picker></mat-datepicker>
        </mat-form-field>

        <!-- Horário -->
        <mat-form-field appearance="outline">
          <mat-label>Horário</mat-label>
          <mat-select [(ngModel)]="selectedSlot" [disabled]="!selectedDate || !selectedDoctorId">
            <mat-option *ngFor="let s of slotOptions" [value]="s">{{ s }}</mat-option>
          </mat-select>
        </mat-form-field>

        <button mat-stroked-button class="agendar-btn"
                [disabled]="!canBook"
                (click)="addAppointment()"
                matTooltip="Agendar">
          <span>Agendar</span>
        </button>
      </mat-card-content>
    </mat-card>

    <!-- Lista de consultas -->
    <section class="list">
      <mat-card class="appt" *ngFor="let appt of appointments; trackBy: trackById">
        <mat-card-title>{{ appt.specialty || 'Consulta' }} • {{ appt.doctor || 'Médico' }}</mat-card-title>
        <mat-card-subtitle>
          {{ appt.date | date:'fullDate' }} — {{ appt.date | date:'HH:mm' }}
        </mat-card-subtitle>
      </mat-card>
      <p *ngIf="appointments.length === 0" class="empty-hint">
        Nenhum agendamento ainda.
      </p>
    </section>
  `,
  styles: [`
    :host { display: block; width: 100%; }
    mat-card { margin: 1rem; padding: 1rem; }
    .form-grid {
      display: grid;
      grid-template-columns: repeat(5, minmax(200px, 1fr));
      gap: 1rem;
      align-items: center;
    }
    .agendar-btn { height: 56px; }
    .list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
      gap: 1rem;
      padding: 0 1rem 1rem;
    }
    .appt { border-left: 4px solid #3f51b5; }
    .empty-hint { grid-column: 1 / -1; opacity: 0.7; margin: 0 1rem 1rem; }
    @media (max-width: 1100px) {
      .form-grid { grid-template-columns: 1fr 1fr; }
      .agendar-btn { width: 100%; }
    }
    @media (max-width: 700px) { .form-grid { grid-template-columns: 1fr; } }
  `]
})
export class AgendaComponent implements OnInit {
  constructor(
    private snack: MatSnackBar,
    private apptService: AppointmentService,
    private medicoService: MedicoService,
    private patientService: PatientService,
    public auth: AuthService
  ) { }

  // paciente padrão (se não admin, vem do JWT)
  private patientId: string | null = null;

  medicos: Medico[] = [];
  especialidades: string[] = [];
  medicosPorEspecialidade: Record<string, Medico[]> = {};
  medById = new Map<string, Medico>();

  // Admin
  patients: Paciente[] = [];
  selectedPatientId: string | null = null;

  today = (() => { const d = new Date(); d.setHours(0, 0, 0, 0); return d; })();

  // formulário
  selectedSpecialty: string | null = null;
  selectedDoctorId: string | null = null;
  selectedDate: Date | null = null;
  selectedSlot: string | null = null; // "HH:mm"

  slotOptions: string[] = [];

  appointments: Appointment[] = [];
  patientInfo: { name: string; email: string; cpf: string } | null = null;

  // ---------- ciclo de vida ----------
  ngOnInit(): void {
    // identifica paciente padrão (não-admin)
    this.patientId = this.auth.isAdmin ? null : this.auth.userId;

    if (!this.auth.isAdmin && !this.patientId) {
      this.snack.open("Sessão expirada. Faça login novamente.", "Fechar", { duration: 3000 });
      return;
    }

    // Carrega médicos (lookup e combos)
    const med$ = this.medicoService.getAllAsync().pipe(
      tap(res => {
        this.medicos = res;
        this.medicosPorEspecialidade = {};
        this.medById = new Map(res.map(m => [m.id, m]));
        res.forEach(m => (this.medicosPorEspecialidade[m.speciality] ||= []).push(m));
        this.especialidades = Object.keys(this.medicosPorEspecialidade);
      })
    );

    if (this.auth.isAdmin) {
      // Admin: carrega lista de pacientes; agenda só após escolher um
      this.patientService.getAll().subscribe({
        next: ps => this.patients = ps,
        error: _ => this.snack.open("Erro ao carregar pacientes", "Fechar", { duration: 3000 })
      });
      med$.subscribe(); // só médicos por enquanto
    } else {
      // Paciente: carrega agenda logo após médicos
      med$.pipe(switchMap(() => this.apptService.getByPatient(this.patientId!)))
        .subscribe({
          next: res => this.applyPatientAppointments(res),
          error: _ => this.snack.open("Erro ao carregar consultas", "Fechar", { duration: 3000 })
        });
    }
  }

  // ---------- UI helpers ----------
  weekdaysFromTodayOnly = (d: Date | null) => {
    if (!d) return false;
    const dd = new Date(d); dd.setHours(0, 0, 0, 0);
    const w = d.getDay(); // 0=Dom, 6=Sáb
    return dd >= this.today && w !== 0 && w !== 6;
  };

  get canBook(): boolean {
    const pid = this.auth.isAdmin ? this.selectedPatientId : this.patientId;
    return !!(pid && this.selectedSpecialty && this.selectedDoctorId && this.selectedDate && this.selectedSlot);
  }

  onPatientChange() {
    // Admin escolheu paciente -> carrega agenda dele
    if (!this.selectedPatientId) return;
    this.apptService.getByPatient(this.selectedPatientId).subscribe({
      next: res => this.applyPatientAppointments(res),
      error: _ => this.snack.open("Erro ao carregar consultas do paciente", "Fechar", { duration: 3000 })
    });
  }

  onSpecialtyChange() {
    this.selectedDoctorId = null;
    this.refreshSlots();
  }

  doctorsForSelected(): Medico[] {
    return this.selectedSpecialty
      ? (this.medicosPorEspecialidade[this.selectedSpecialty] ?? [])
      : [];
  }

  refreshSlots() {
    this.selectedSlot = null;
    this.slotOptions = [];
    if (!this.selectedDate || !this.selectedDoctorId) return;

    this.apptService.getSchedule(this.selectedDoctorId, this.selectedDate).subscribe({
      next: slots => {
        this.slotOptions = slots
          .filter(s => s.available)
          .map(s => this.timeToStr(new Date(s.hour)))
          .sort((a, b) => a.localeCompare(b));
      },
      error: _ => this.snack.open("Erro ao buscar agenda", "Fechar", { duration: 3000 })
    });
  }

  trackById = (_: number, x: { id: string }) => x.id;

  // ---------- map/format ----------
  private applyPatientAppointments(res: any) {
    this.patientInfo = { name: res.name, email: res.email, cpf: res.cpf };
    this.appointments = (res.schedulles ?? []).map((s: any) => {
      const med = this.medById.get(s.healthcareId);
      return {
        id: s.appointmentId ?? `${s.healthcareId}-${s.hour}`,
        patientId: s.patientId,
        date: new Date(s.hour),
        specialty: med?.speciality ?? "",
        doctor: med?.name ?? ""
      };
    });
  }

  private timeToStr(d: Date): string {
    return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
  }

  // junta data + "HH:mm" e zera segundos
  private mergeDateAndTime(baseDate: Date, hhmm: string): Date {
    const [hh, mm] = hhmm.split(':').map(n => +n);
    const d = new Date(baseDate);
    d.setHours(hh, mm, 0, 0);
    return d; // local
  }

  // ISO local SEM 'Z' e SEM offset (back espera local puro)
  private toLocalIsoNoTz(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    const hh = String(d.getHours()).padStart(2, '0');
    const mm = String(d.getMinutes()).padStart(2, '0');
    const ss = "00";
    return `${y}-${m}-${dd}T${hh}:${mm}:${ss}`;
  }

  private toYYYYMMDD(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${dd}`;
  }

  // ---------- ação ----------
  addAppointment(): void {
    if (!this.canBook) return;

    const dd = new Date(this.selectedDate!); dd.setHours(0, 0, 0, 0);
    if (dd < this.today) {
      this.snack.open("Não é permitido agendar em data passada.", "Fechar", { duration: 3000 });
      return;
    }
    if (!this.slotOptions.includes(this.selectedSlot!)) {
      this.snack.open("Horário inválido para o dia selecionado.", "Fechar", { duration: 3500 });
      return;
    }

    const pid = this.auth.isAdmin ? this.selectedPatientId! : this.patientId!;
    const day = this.toYYYYMMDD(this.selectedDate!); // "YYYY-MM-DD"
    const hour = this.toLocalIsoNoTz(this.mergeDateAndTime(this.selectedDate!, this.selectedSlot!)); // "YYYY-MM-DDTHH:mm:00"

    const body = { healthcareId: this.selectedDoctorId!, patientId: pid, day, hour };

    this.apptService.agendar(body).subscribe({
      next: () => {
        this.snack.open("Consulta agendada!", "Fechar", { duration: 2500 });
        this.refreshSlots();
        this.onPatientChange(); // recarrega lista do paciente atual
      },
      error: err => this.snack.open(err?.error?.detail ?? "Erro ao agendar consulta", "Fechar", { duration: 4000 })
    });
  }
}
