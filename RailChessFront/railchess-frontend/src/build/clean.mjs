import { existsSync, rmSync, mkdirSync } from 'fs';

const folderPath = '../../RailChess/wwwroot/assets';

export function clean(){
    try {
        if (existsSync(folderPath)) {
            rmSync(folderPath, { recursive: true });
            console.log('成功删除assets目录');
        }
        mkdirSync(folderPath, { recursive: true });
    } catch (err) {
        console.error('assets目录删除失败：', err);
    }
}