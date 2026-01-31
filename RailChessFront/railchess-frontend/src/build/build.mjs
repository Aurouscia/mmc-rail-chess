import { execa } from 'execa'
import { clean } from './clean.mjs'

clean()

const firstParam = process.argv[2]
const buildHere = firstParam?.toLowerCase() === '--here'
const viteBuildArgs = buildHere ? ['--outDir', './dist'] : []
const execaArgs = { stdout: 'inherit', stderr: 'inherit', reject: false }

const viteBuild = execa("npx", ["vite", "build", ...viteBuildArgs], execaArgs);
const vueTsc = execa("npx", ["vue-tsc"], execaArgs)
await Promise.all([vueTsc, viteBuild])