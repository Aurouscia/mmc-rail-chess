import { existsSync, rmSync, mkdirSync } from 'fs';

const folderPath = '../../RailChess/wwwroot/assets';

export function clean(){
    try {
        if (existsSync(folderPath)) {
            rmSync(folderPath, { recursive: true });
            console.log('assets folder removed');
        }
        mkdirSync(folderPath, { recursive: true });
    } catch (err) {
        console.error('assets folder removal failed', err);
    }
}