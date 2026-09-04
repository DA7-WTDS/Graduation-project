import { useQuery } from '@tanstack/react-query'
import { fetchShadowTrackRecord } from './shadowTrackRecordApi'

/** The model-portfolio track record. Advances once a night, so it is cached hard. */
export function useShadowTrackRecord() {
    return useQuery({
        queryKey: ['shadow-track-record'],
        queryFn: fetchShadowTrackRecord,
        staleTime: 1000 * 60 * 30,
    })
}
