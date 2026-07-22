import { Platform } from 'react-native'
import * as SecureStore from 'expo-secure-store'

/**
 * Token storage that works on device and in Expo web.
 *
 * On native we use the OS keychain via expo-secure-store (the auth token is a
 * credential, not casual state). expo-secure-store has no web implementation, so
 * on web we fall back to localStorage — used only for local development preview.
 */
const isWeb = Platform.OS === 'web'

export async function getItem(key: string): Promise<string | null> {
    if (isWeb) {
        return globalThis.localStorage?.getItem(key) ?? null
    }
    return SecureStore.getItemAsync(key)
}

export async function setItem(key: string, value: string): Promise<void> {
    if (isWeb) {
        globalThis.localStorage?.setItem(key, value)
        return
    }
    await SecureStore.setItemAsync(key, value)
}

export async function deleteItem(key: string): Promise<void> {
    if (isWeb) {
        globalThis.localStorage?.removeItem(key)
        return
    }
    await SecureStore.deleteItemAsync(key)
}
