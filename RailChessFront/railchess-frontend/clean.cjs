const fs = require('fs');

const folderPath = '../../RailChess/wwwroot/assets';

try {
    if (fs.existsSync(folderPath)) {
        fs.rmSync(folderPath, { recursive: true });
        console.log('assets folder deleted successfully.');
    }
    fs.mkdirSync(folderPath);
} catch (err) {
    console.error('Error deleting folder:', err);
}