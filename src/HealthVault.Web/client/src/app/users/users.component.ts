import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PeopleService } from './people.service';
import { Person } from './person.model';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  private readonly peopleService = inject(PeopleService);

  people: Person[] = [];
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.peopleService.getPeople().subscribe({
      next: (people) => {
        this.people = people;
        this.loading = false;
      },
      error: () => {
        this.error = 'Unable to load people.';
        this.loading = false;
      }
    });
  }
}
