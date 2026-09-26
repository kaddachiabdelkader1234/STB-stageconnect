import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiError } from '../../../core/http/api-error';

@Component({
  selector: 'app-sign-in',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './sign-in.component.html',
  styleUrls: ['./sign-in.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SignInComponent {
  signInForm: FormGroup;
  showPassword = false;
  rememberMe = false;
  isLoading = false;
  errorMessage = '';
  showForgotPassword = false;
  forgotEmail = '';
  isForgotLoading = false;
  forgotSuccess = '';
  forgotError = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.signInForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  openForgotPassword(event: Event) {
    event.preventDefault();
    this.showForgotPassword = true;
    this.forgotEmail = this.signInForm.value.email || '';
    this.forgotSuccess = '';
    this.forgotError = '';
  }

  closeForgotPassword() {
    this.showForgotPassword = false;
    this.forgotSuccess = '';
    this.forgotError = '';
  }

  submitForgotPassword() {
    if (!this.forgotEmail || this.isForgotLoading) return;

    this.isForgotLoading = true;
    this.forgotSuccess = '';
    this.forgotError = '';

    this.authService.forgotPassword(this.forgotEmail).subscribe({
      next: (res) => {
        this.isForgotLoading = false;
        this.forgotSuccess = res.message || 'Si un compte existe, un nouveau mot de passe a été envoyé par email.';
      },
      error: (err) => {
        this.isForgotLoading = false;
        this.forgotError = err.message || 'Une erreur est survenue lors de la réinitialisation.';
      }
    });
  }

  onSubmit() {
    if (this.signInForm.valid && !this.isLoading) {
      this.isLoading = true;
      this.errorMessage = '';

      const loginData = {
        username: this.signInForm.value.email,
        password: this.signInForm.value.password
      };

      this.authService.login(loginData).subscribe({
        next: () => {
          // Only reached on a real 2xx now — AuthService rethrows failures instead of
          // converting them into a normal emission, which used to land here with no token.
          this.isLoading = false;
          this.router.navigate(this.authService.hasRole('ADMIN') ? ['/admin'] : ['/espace']);
        },
        error: (error: ApiError) => {
          this.isLoading = false;
          this.errorMessage = error.message;
        }
      });
    }
  }
}
