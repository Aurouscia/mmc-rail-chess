/**
 * API 响应解密工具
 * 对应后端 AES-256-GCM 加密，密钥由 .env 中三段拼接后经 SHA-256 派生
 */

function getRawKey(): string {
    const p1 = import.meta.env.VITE_ENC_KEY_P1 || '';
    const p2 = import.meta.env.VITE_ENC_KEY_P2 || '';
    const p3 = import.meta.env.VITE_ENC_KEY_P3 || '';
    return p1 + p2 + p3;
}

async function deriveKey(rawKey: string): Promise<CryptoKey> {
    const encoder = new TextEncoder();
    const data = encoder.encode(rawKey);
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    return crypto.subtle.importKey(
        'raw',
        hashBuffer,
        { name: 'AES-GCM' },
        false,
        ['decrypt']
    );
}

function base64ToArrayBuffer(base64: string): ArrayBuffer {
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
}

let cachedKey: CryptoKey | null = null;

async function getKey(): Promise<CryptoKey> {
    if (cachedKey) return cachedKey;
    const raw = getRawKey();
    if (!raw) {
        throw new Error('未配置 VITE_ENC_KEY_P1/P2/P3，无法解密 API 响应');
    }
    cachedKey = await deriveKey(raw);
    return cachedKey;
}

/**
 * 解密后端返回的 AES-256-GCM 密文
 * @param cipherText 格式：Base64(iv).Base64(cipher).Base64(tag)
 */
export async function decryptApiData(cipherText: string): Promise<any> {
    const parts = cipherText.split('.');
    if (parts.length !== 3) {
        throw new Error('密文格式错误，应为 iv.cipher.tag');
    }

    const iv = new Uint8Array(base64ToArrayBuffer(parts[0]));
    const cipher = new Uint8Array(base64ToArrayBuffer(parts[1]));
    const tag = new Uint8Array(base64ToArrayBuffer(parts[2]));

    // Web Crypto API 期望 ciphertext 与 tag 拼接
    const combined = new Uint8Array(cipher.length + tag.length);
    combined.set(cipher, 0);
    combined.set(tag, cipher.length);

    const key = await getKey();
    const decrypted = await crypto.subtle.decrypt(
        { name: 'AES-GCM', iv },
        key,
        combined
    );

    const decoder = new TextDecoder();
    return JSON.parse(decoder.decode(decrypted));
}
