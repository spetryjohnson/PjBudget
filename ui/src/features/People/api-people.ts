import { apiClient } from '../../lib/apiClient'

export interface Person {
	id: number
	version: string
	displayName: string
	birthDate: string | null
	sortOrder: number
}

export function newPerson(): Person {
	return { id: 0, version: '00000000-0000-0000-0000-000000000000', displayName: '', birthDate: null, sortOrder: 0 }
}

export const peopleApi = {
	list: () => apiClient.get('api/people').json<Person[]>(),
	create: (person: Person) => apiClient.post('api/people', { json: person }).json<Person>(),
	update: (person: Person) => apiClient.put(`api/people/${person.id}`, { json: person }).json<Person>(),
	remove: (id: number) => apiClient.delete(`api/people/${id}`),
}
