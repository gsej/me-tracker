import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-api-key-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './api-key-input.component.html',
  styleUrl: './api-key-input.component.scss'
})
export class ApiKeyInputComponent implements OnInit {
  apiKey: string = '';
  isVisible: boolean = false;
  private readonly STORAGE_KEY = 'api_key';

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    const apiKeyInput = document.querySelector('app-api-key-input');
    
    // Only hide if clicking outside the component
    if (apiKeyInput && !apiKeyInput.contains(target)) {
      this.isVisible = false;
    }
  }

  ngOnInit() {
    this.loadApiKey();
  }

  toggleVisibility() {
    this.isVisible = !this.isVisible;
  }

  saveApiKey() {
    localStorage.setItem(this.STORAGE_KEY, this.apiKey);
  }

  private loadApiKey() {
    const storedKey = localStorage.getItem(this.STORAGE_KEY);
    if (storedKey) {
      this.apiKey = storedKey;
    }
  }
}
