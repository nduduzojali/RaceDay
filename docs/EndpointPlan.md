| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or Participant. | None (public) | { fullName, email, password, role, phoneNumber } | 201 Created – user created; 409 Conflict – email already in use |
| POST | /api/auth/login | Authenticates a user and returns an access token. | None (public) | { email, password } | 200 OK – token returned; 401 Unauthorized – invalid credentials |
| GET | /api/users/me | Returns the logged-in user's profile details. | Any (logged in) | None | 200 OK – user profile; 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's profile details. | Any (logged in) | { fullName, phoneNumber } | 200 OK – profile updated; 400 Bad Request |
| POST | /api/events | Creates a new event. | Organiser | { eventName, eventDate, location, description } | 201 Created – event created; 403 Forbidden – not an organiser |
| GET | /api/events | Lists all upcoming events, browsable by anyone logged in. | Any (logged in) | None | 200 OK – list of events |
| GET | /api/events/{id} | Returns full details of a specific event. | Any (logged in) | None | 200 OK – event details; 404 Not Found |
| PUT | /api/events/{id} | Updates an existing event. | Organiser | { eventName, eventDate, location, description } | 200 OK – event updated; 403 Forbidden; 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event. | Organiser | None | 200 OK – event deleted; 403 Forbidden; 404 Not Found |
| POST | /api/events/{id}/categories | Creates a new category (e.g. 10km, 21km) under an event. | Organiser | { categoryName, maxParticipants, entryFee } | 201 Created – category created; 404 Not Found – event does not exist |
| GET | /api/events/{id}/categories | Lists all categories for a specific event. | Any (logged in) | None | 200 OK – list of categories; 404 Not Found |
| POST | /api/categories/{id}/enrolments | Enrols the logged-in participant into a category for an event. | Participant | { } (uses logged-in user) | 201 Created – enrolment recorded; 404 Not Found – category does not exist; 409 Conflict – already enrolled |
| GET | /api/enrolments/me | Returns all enrolments belonging to the logged-in participant. | Participant | None | 200 OK – list of enrolments |
| GET | /api/events/{id}/enrolments | Returns all enrolments for an event (organiser oversight). | Organiser | None | 200 OK – list of enrolments; 403 Forbidden |
| POST | /api/enrolments/{id}/results | Captures a participant's finish time and position for their enrolment. | Organiser | { finishTime, position } | 201 Created – result recorded; 404 Not Found – enrolment does not exist |
| GET | /api/enrolments/{id}/results | Returns the result for a specific enrolment. | Any (logged in) | None | 200 OK – result details; 404 Not Found – no result yet |