import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'localTime', standalone: true })
export class LocalTimePipe implements PipeTransform {
  transform(value: string): string {
    return new Date(value).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }
}
