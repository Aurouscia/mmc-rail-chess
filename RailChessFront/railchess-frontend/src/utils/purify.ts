import DOMPurify from "dompurify"

export function purify(dirtyHtml: string) {
    return DOMPurify.sanitize(dirtyHtml, {
        ALLOWED_TAGS: ['b', 'i', 'u', 'em', 'strong', 'br', 'p'],
        ALLOWED_ATTR: [] // 不允许任何属性（防 onclick, href="javascript:" 等）
    })
}