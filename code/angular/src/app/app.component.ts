import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SettingsService } from './settings/settings.service';
import { PiComponent } from './components/pi/pi.component';
import { ApiKeyInputComponent } from './components/api-key-input/api-key-input.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    PiComponent,
    ApiKeyInputComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'me-tracker';

  public gitHash: string = 'not set';

  constructor(settingsService: SettingsService) {
    this.gitHash = settingsService.settings.gitHash;
  }
}
