import { execa } from 'execa'
import { clean } from './clean.mjs'

clean()

const firstParam = process.argv[2]
const buildHere = firstParam?.toLowerCase() === '--here'
const viteBuildArgs = buildHere ? ['--outDir', './dist'] : []

const viteBuild = execa("vite build", viteBuildArgs, {stdout:'inherit', reject:false});
const vueTsc = execa("vue-tsc", {stdout:'inherit', reject:false})
await Promise.all([vueTsc, viteBuild])