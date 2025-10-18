import { execa } from 'execa'
import { clean } from './clean.mjs'

clean()

const firstParam = process.argv[2]
const buildHere = firstParam?.toLowerCase() === '--here'
const viteBuildArgs = buildHere ? ['--outDir', './dist'] : []

const viteBuild = execa("npx", ["vite", "build", ...viteBuildArgs], {stdout:'inherit', reject:true});
const vueTsc = execa("npx", ["vue-tsc"], {stdout:'inherit', reject:true})
await Promise.all([vueTsc, viteBuild])