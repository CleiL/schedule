import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { AgendaSlotDto } from "../interfaces/agenda";
import { DoctorSchedulesResponseDto } from "../interfaces/consulta";
import { CreateAppointmentDto, PatientSchedulesResponseDto } from "../interfaces/appointment";
import { environment } from "../../environments/environment.prod";

export interface SchedullSlotDto {
  hour: string;     // vem ISO do backend (DateTime)
  available: boolean;
}

@Injectable({ providedIn: "root" })
export class AppointmentService {

  private apiUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) { }

  /** Lista os slots disponíveis (30min) para o médico no dia */
  getAgenda(id: string, day: Date): Observable<AgendaSlotDto[]> {
    const diaParam = day.toISOString().split("T")[0]; // apenas YYYY-MM-DD
    return this.http.get<AgendaSlotDto[]>(`${this.apiUrl}/Appointments/professional/${id}/consultations`, {
      params: { dia: diaParam }
    });
  }

  getSchedule(id: string, day: Date): Observable<SchedullSlotDto[]> {
    const dayParam = day.toISOString().slice(0, 10); // YYYY-MM-DD
    return this.http.get<SchedullSlotDto[]>(
      `${this.apiUrl}/Appointments/professional/${id}/schedule`,
      { params: { day: dayParam } }
    );
  }

  agendar(dto: CreateAppointmentDto): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Appointments`, dto);
  }

  /** Lista todas as consultas do médico */
  getByDoctor(healthcareId: string): Observable<DoctorSchedulesResponseDto> {
    return this.http.get<DoctorSchedulesResponseDto>(
      `${this.apiUrl}/Healthcares/healthcare/${healthcareId}/schedules`
    );
  }

  /** Lista todas as consultas do paciente */
  getByPatient(id: string): Observable<PatientSchedulesResponseDto> {
    return this.http.get<PatientSchedulesResponseDto>(
      `${this.apiUrl}/Patients/patients/${id}/appointments`
    );
  }



}
