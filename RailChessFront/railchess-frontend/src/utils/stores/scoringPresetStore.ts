import { defineStore } from "pinia";
import { persistKey } from "./persistKey";
import { ref } from "vue";
import type { CompetitionMatchScoring } from "../../models/competition";

export interface ScoringPreset {
    name: string;
    scoring: CompetitionMatchScoring;
}

const storeName = 'scoringPresetStore';
export const useScoringPresetStore = defineStore(storeName, () => {
    const presets = ref<ScoringPreset[]>([]);

    function add(preset: ScoringPreset) {
        presets.value = presets.value.filter(p => p.name !== preset.name);
        presets.value.push(preset);
    }

    function remove(name: string) {
        presets.value = presets.value.filter(p => p.name !== name);
    }

    return {
        presets,
        add,
        remove
    };
}, {
    persist: {
        key: persistKey(storeName)
    }
});
