import { useQuery } from '@tanstack/react-query'
import { fetchTrackRecord, type TrackRecord } from './trackRecordApi'

/** The public realized track record. Refetched rarely — it only moves when the
 * nightly outcome scorer runs. */
export function useTrackRecord() {
    return useQuery<TrackRecord>({
        queryKey: ['trackRecord'],
        queryFn: fetchTrackRecord,
        staleTime: 1000 * 60 * 60,
    })
}
