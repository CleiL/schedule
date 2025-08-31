import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

export type ProfileType = 'paciente' | 'medico';

export interface ProfileDto {
  name: string;
  phone?: string;
  email: string;
  cpf?: string;
  crm?: string;
  specialty?: string;
}

export interface IProfileService {
  getProfile(): Observable<ProfileDto>;
  updateProfile(payload: Partial<ProfileDto>): Observable<void>;
  updatePassword(payload: { password: string }): Observable<void>;
}

export const PROFILE_SERVICE = new InjectionToken<IProfileService>('PROFILE_SERVICE');
export const PROFILE_TYPE = new InjectionToken<ProfileType>('PROFILE_TYPE');
