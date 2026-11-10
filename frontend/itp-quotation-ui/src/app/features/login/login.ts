import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <main class="login-page">
      <form #form="ngForm" (ngSubmit)="form.valid && submit()">
        <div class="brand">ITP · Quotation</div>
        <p class="eyebrow">Indhra Turning Point</p>
        <h1>{{ registering ? 'Create your account' : 'Welcome back' }}</h1>
        <p>{{ registering ? 'Register for read-only access to the quotation workspace.' : 'Sign in to your quotation workspace.' }}</p>
        @if (registering) {
          <mat-form-field appearance="outline">
            <mat-label>Display name</mat-label>
            <input matInput name="displayName" [(ngModel)]="displayName" maxlength="100" autocomplete="name" [disabled]="busy()">
          </mat-form-field>
        }
        <mat-form-field appearance="outline">
          <mat-label>Username</mat-label>
          <input matInput name="username" [(ngModel)]="username" required maxlength="100" autocomplete="username" [disabled]="busy()">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Password</mat-label>
          <input matInput type="password" name="password" [(ngModel)]="password" required [minlength]="registering ? 8 : 1" [autocomplete]="registering ? 'new-password' : 'current-password'" [disabled]="busy()">
        </mat-form-field>
        @if (registering) { <p class="help password-help">Use at least 8 characters.</p> }
        @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
        <button mat-flat-button type="submit" [disabled]="form.invalid || busy()">{{ busy() ? (registering ? 'Creating account…' : 'Signing in…') : (registering ? 'Create account' : 'Sign in') }}</button>
        <p class="help">
          {{ registering ? 'Already have an account?' : 'Need access?' }}
          <button mat-button type="button" class="mode-toggle" [disabled]="busy()" (click)="toggleMode()">{{ registering ? 'Sign in' : 'Create an account' }}</button>
        </p>
      </form>
    </main>`,
  styles: `
    .login-page { min-height: 100dvh; display: grid; place-items: center; padding: 24px; background: #f3f6fa; }
    form { width: min(100%, 420px); padding: 36px; background: white; border: 1px solid #dde3ec; border-radius: 20px; box-shadow: 0 20px 60px #16305512; }
    .brand { font-size: 22px; font-weight: 800; color: #183b63; margin-bottom: 36px; }
    h1 { font-size: 30px; margin: 8px 0; } p { color: #58677b; line-height: 1.6; }
    mat-form-field, button { width: 100%; } mat-form-field { margin-top: 12px; }
    .help { font-size: 13px; margin-top: 24px; } .password-help { margin: -4px 0 8px; } .error { color: #b42318; }
    .mode-toggle { min-width: 0; padding: 0 4px; }
    @media (max-width: 480px) { form { padding: 24px; } }
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  registering = false;
  username = '';
  password = '';
  displayName = '';
  readonly busy = signal(false);
  readonly error = signal('');

  toggleMode() {
    this.registering = !this.registering;
    this.password = '';
    this.error.set('');
  }

  submit() {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    const request = this.registering
      ? this.auth.register(this.username.trim(), this.password, this.displayName.trim())
      : this.auth.login(this.username.trim(), this.password);
    request.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: () => {
        this.password = '';
        const target = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
        void this.router.navigateByUrl(target.startsWith('/') && !target.startsWith('//') && !target.startsWith('/login') ? target : '/dashboard');
      },
      error: error => this.error.set(this.registering
        ? (typeof error.error === 'string' ? error.error : 'Unable to create the account. Please check your details and try again.')
        : (error.status === 401 || error.status === 400
          ? 'The username or password is incorrect.' : 'Unable to sign in. Please try again.')),
    });
  }
}
