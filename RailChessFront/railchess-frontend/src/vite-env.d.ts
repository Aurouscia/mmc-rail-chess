/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_ENC_KEY_P1: string
    readonly VITE_ENC_KEY_P2: string
    readonly VITE_ENC_KEY_P3: string
}

interface ImportMeta {
    readonly env: ImportMetaEnv
}
