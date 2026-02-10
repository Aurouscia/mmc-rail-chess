import { existsSync, rmSync, mkdirSync } from 'fs';

const folderPaths = ['../../RailChess/wwwroot/assets', 'dist'];

export function clean(){
    for(const folderPath of folderPaths){
        try {
            if (existsSync(folderPath)) {
                rmSync(folderPath, { recursive: true });
                console.log('assets folder removed：', folderPath);
            }
            mkdirSync(folderPath, { recursive: true });
        } catch (err) {
            console.error('assets folder removal failed', err);
        }
    }
}