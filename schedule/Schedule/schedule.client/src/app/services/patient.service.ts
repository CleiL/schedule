import { Injectable } from '@angular/core';
import { IProfileService, ProfileDto } from '../contracts/profile.tokens';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface Paciente {
  id: string;
  name: string;
  email: string;
  cpf: string;
}

@Injectable({ providedIn: 'root' })
export class PatientService implements IProfileService {
  constructor(private http: HttpClient) { }
  getProfile(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>('/api/pacientes/me');
  }
  updateProfile(payload: Partial<ProfileDto>): Observable<void> {
    return this.http.put<void>('/api/pacientes/me', payload);
  }
  updatePassword(payload: { password: string }): Observable<void> {
    return this.http.post<void>('/api/pacientes/me/password', payload);
  }

  private apiUrl = environment.apiUrls[0];

  getById(id: string): Observable<Paciente> {
    // Ajuste o path se o seu controller for diferente
    return this.http.get<Paciente>(`${this.apiUrl}/Patients/${id}`);
  }

  getAll(): Observable<Paciente[]> {
    // ajuste a rota conforme seu backend
    return this.http.get<Paciente[]>(`${this.apiUrl}/Patients`);
  }
}
