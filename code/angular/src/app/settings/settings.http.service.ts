import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { SettingsService } from "./settings.service";
import { Settings } from "./settings";
import { firstValueFrom } from "rxjs";

@Injectable({ providedIn: 'root' })
export class SettingsHttpService {

  constructor(private http: HttpClient, private settingsService: SettingsService) {
  }

  initializeApp(): Promise<any> {

    return new Promise(
      (resolve) => {
        firstValueFrom(this.http.get('settings.json'))
          .then(response => {
            this.settingsService.settings = <Settings>response;
            resolve(null);
          }
          )
      }
    );
  }
}
