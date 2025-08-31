import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { Component, inject, OnInit } from "@angular/core";
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatTabsModule } from "@angular/material/tabs";
import { PROFILE_SERVICE, PROFILE_TYPE, ProfileType, IProfileService } from "../contracts/profile.tokens";
import { patternValidator, cpfPattern, crmPattern } from "../validators/validators";

@Component({
  selector: "app-profile",
  standalone: true,
  imports: [
    CommonModule, RouterModule, ReactiveFormsModule,
    MatTabsModule, MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule
  ],
  providers: [],
  template: `
    <section class="user-manager">
      <h1>Perfil {{ profileType === 'medico' ? 'do Médico' : 'do Usuário' }}</h1>
      <p>Gerencia permissões, acessos e segurança.</p>

      <mat-tab-group>
        <mat-tab [label]="profileType === 'medico' ? 'Médico' : 'Usuário'">
          <mat-card appearance="outlined">
            <mat-card-header>
              <mat-card-title>Perfil</mat-card-title>
              <mat-card-subtitle>Gestão do Perfil</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <form [formGroup]="userForm">
                <mat-form-field appearance="outline">
                  <mat-label>Nome</mat-label>
                  <input matInput formControlName="name" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Telefone</mat-label>
                  <input matInput formControlName="phone" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>E-mail</mat-label>
                  <input matInput formControlName="email" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>CPF</mat-label>
                  <input matInput formControlName="cpf" />
                  <mat-error *ngIf="userForm.get('cpf')?.hasError('cpf')">CPF inválido</mat-error>
                </mat-form-field>

                <!-- Campos extras do médico -->
                <ng-container *ngIf="profileType === 'medico'">
                  <mat-form-field appearance="outline">
                    <mat-label>CRM</mat-label>
                    <input matInput formControlName="crm" placeholder="CRM/SP 123456" />
                    <mat-error *ngIf="userForm.get('crm')?.hasError('crm')">CRM no formato CRM/UF 123456</mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline">
                    <mat-label>Especialidade</mat-label>
                    <input matInput formControlName="specialty" />
                  </mat-form-field>
                </ng-container>
              </form>
            </mat-card-content>

            <mat-card-actions>
              <ng-container *ngIf="editingUser; else editUserBtn">
                <button mat-flat-button color="primary" (click)="onSaveUser()">Salvar</button>
                <button mat-stroked-button (click)="onCancel()">Cancelar</button>
              </ng-container>
              <ng-template #editUserBtn>
                <button mat-flat-button color="accent" (click)="enableUserForm()">Editar</button>
              </ng-template>
            </mat-card-actions>
          </mat-card>
        </mat-tab>

        <mat-tab label="Segurança">
          <mat-card appearance="outlined">
            <mat-card-header>
              <mat-card-title>Segurança</mat-card-title>
              <mat-card-subtitle>Gestão de Segurança</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <form [formGroup]="securityForm">
                <mat-form-field appearance="outline">
                  <mat-label>Senha</mat-label>
                  <input matInput type="password" formControlName="password" />
                </mat-form-field>
              </form>
            </mat-card-content>
            <mat-card-actions>
              <ng-container *ngIf="editingSecurity; else editSecurityBtn">
                <button mat-flat-button color="primary" (click)="onSavePassword()">Salvar</button>
                <button mat-stroked-button (click)="onCancel()">Cancelar</button>
              </ng-container>
              <ng-template #editSecurityBtn>
                <button mat-flat-button color="accent" (click)="enableSecurityForm()">Editar</button>
              </ng-template>
            </mat-card-actions>
          </mat-card>
        </mat-tab>
      </mat-tab-group>
    </section>
  `,
  styles: [`
    :host { display:block; padding:16px; border-radius:8px; }
    .user-manager { margin:1rem; }
    mat-card { margin-top:1rem; }
    mat-card-title { font-size:1.5rem; font-weight:bold; }
    mat-card-subtitle { font-size:1rem; margin-bottom:0.5rem; }
    mat-card-actions { display:flex; gap:0.5rem; justify-content:flex-end; }
    mat-form-field { width:100%; margin-bottom:1rem; }
  `]
})
export class ProfileComponent implements OnInit {
  userForm!: FormGroup;
  securityForm!: FormGroup;
  loading = false;
  editingUser = false;
  editingSecurity = false;

  private fb = inject(FormBuilder);
  private service = inject<IProfileService>(PROFILE_SERVICE);
  profileType = inject(PROFILE_TYPE); // 'paciente' | 'medico'

  ngOnInit() {
    this.userForm = this.fb.group({
      name: [{ value: '', disabled: true }, [Validators.required]],
      phone: [{ value: '', disabled: true }],
      email: [{ value: '', disabled: true }, [Validators.required, Validators.email]],
      cpf: [{ value: '', disabled: true }, [patternValidator(cpfPattern, 'cpf')]],
      // adicionados dinamicamente se for médico
      crm: [{ value: '', disabled: true }],
      specialty: [{ value: '', disabled: true }]
    });

    if (this.profileType === 'medico') {
      this.userForm.get('crm')?.addValidators(patternValidator(crmPattern, 'crm'));
      this.userForm.get('specialty')?.addValidators(Validators.required);
    }

    this.securityForm = this.fb.group({
      password: [{ value: '*****************', disabled: true }, [Validators.required, Validators.minLength(8)]]
    });

    this.loadProfile();
  }

  private loadProfile() {
    this.loading = true;
    this.service.getProfile().subscribe({
      next: (p) => { this.userForm.patchValue(p); this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  onSaveUser() {
    if (this.userForm.invalid) return;
    const payload = this.userForm.getRawValue();
    this.service.updateProfile(payload).subscribe({
      next: () => { alert('Dados atualizados com sucesso!'); this.editingUser = false; this.userForm.disable(); },
      error: () => alert('Erro ao atualizar dados.')
    });
  }

  onSavePassword() {
    if (this.securityForm.invalid) return;
    this.service.updatePassword(this.securityForm.value).subscribe({
      next: () => { alert('Senha atualizada com sucesso!'); this.editingSecurity = false; this.securityForm.disable(); },
      error: () => alert('Erro ao atualizar a senha.')
    });
  }

  enableUserForm() { this.editingUser = true; this.userForm.enable(); }
  enableSecurityForm() { this.editingSecurity = true; this.securityForm.enable(); }
  onCancel() {
    this.editingUser = false; this.editingSecurity = false;
    this.userForm.disable(); this.securityForm.disable();
    this.loadProfile();
  }
}
