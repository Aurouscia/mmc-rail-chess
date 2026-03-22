<script lang="ts" setup>
import { reactive, computed, watch } from 'vue';

type LinkModeValue = 'Group' | 'Connect' | 'None';
type LinkModeKey = 'ThickLine' | 'ThinLine' | 'DottedLine1' | 'DottedLine2' | 'Group';

interface LinkMode {
  ThickLine: LinkModeValue;
  ThinLine: LinkModeValue;
  DottedLine1: LinkModeValue;
  DottedLine2: LinkModeValue;
  Group: LinkModeValue;
}

interface Config {
  max_length: number;
  merge_consecutive_duplicates: boolean;
  link_modes: LinkMode;
  friend_lines: string;
  merged_lines: string;
  max_rc_steps: number;
  optimize_segmentation: boolean;
  optimize_iterations: number;
  segmented_lines: string;
}

const props = defineProps<{
  config: string;
}>();

const emit = defineEmits<{
  (e: 'update:config', value: string): void;
}>();

const config = reactive<Config>({
  max_length: 128,
  merge_consecutive_duplicates: true,
  link_modes: {
    ThickLine: 'Connect',
    ThinLine: 'Connect',
    DottedLine1: 'None',
    DottedLine2: 'None',
    Group: 'Group',
  },
  friend_lines: '',
  merged_lines: '',
  max_rc_steps: 16,
  optimize_segmentation: false,
  optimize_iterations: 5,
  segmented_lines: '',
});

const linkModeOptions: { value: LinkModeValue; label: string }[] = [
  { value: 'Group', label: '合并' },
  { value: 'Connect', label: '连接' },
  { value: 'None', label: '忽略' },
];

const linkModeRows: { key: LinkModeKey; label: string }[] = [
  { key: 'ThickLine', label: '粗线' },
  { key: 'ThinLine', label: '细线' },
  { key: 'DottedLine1', label: '虚线(原色)' },
  { key: 'DottedLine2', label: '虚线(覆盖)' },
  { key: 'Group', label: '车站团' },
];

const configJson = computed(() => JSON.stringify(config));

watch(configJson, (newVal) => {
  emit('update:config', newVal);
}, { immediate: true });
</script>

<template>
  <div class="options">
    <div class="item">
      <div class="label">
        <div class="title">最大长度</div>
        <div class="key">max_length</div>
      </div>
      <input type="number" v-model.number="config.max_length" min="1" />
    </div>
    <div class="item">
      <div class="label">
        <div class="title">合并连续重复站</div>
        <div class="key">merge_consecutive_duplicates</div>
      </div>
      <select v-model="config.merge_consecutive_duplicates">
        <option :value="true">是</option>
        <option :value="false">否</option>
      </select>
    </div>
    <div class="item">
      <div class="label">
        <div class="title">连线处理方式</div>
        <div class="key">link_modes</div>
      </div>
      <div class="link-modes">
        <div v-for="row in linkModeRows" :key="row.key" class="link-item">
          <span>{{ row.label }}</span>
          <select v-model="config.link_modes[row.key]">
            <option v-for="opt in linkModeOptions" :key="opt.value" :value="opt.value">
              {{ opt.label }}
            </option>
          </select>
        </div>
      </div>
    </div>
    <div class="item">
      <div class="label">
        <div class="title">可跨线运行线路</div>
        <div class="key">friend_lines</div>
      </div>
      <input type="text" v-model="config.friend_lines" placeholder="线路编号或名称，多个用逗号分隔" />
    </div>
    <div class="item">
      <div class="label">
        <div class="title">无视朝向跨线线路</div>
        <div class="key">merged_lines</div>
      </div>
      <input type="text" v-model="config.merged_lines" placeholder="线路编号或名称，多个用逗号分隔" />
    </div>
    <div class="item">
      <div class="label">
        <div class="title">分段处理随机数最大值</div>
        <div class="key">max_rc_steps</div>
      </div>
      <input type="number" v-model.number="config.max_rc_steps" min="1" />
    </div>
    <div class="item">
      <div class="label">
        <div class="title">开启分段处理优化</div>
        <div class="key">optimize_segmentation</div>
      </div>
      <select v-model="config.optimize_segmentation">
        <option :value="true">true</option>
        <option :value="false">false</option>
      </select>
    </div>
    <div class="item">
      <div class="label">
        <div class="title">优化迭代次数</div>
        <div class="key">optimize_iterations</div>
      </div>
      <input type="number" v-model.number="config.optimize_iterations" min="1" />
    </div>
    <div class="item">
      <div class="label">
        <div class="title">强制分段处理线路</div>
        <div class="key">segmented_lines</div>
      </div>
      <input type="text" v-model="config.segmented_lines" placeholder="格式: 线路名:长度,多个用逗号分隔" />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.options {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  background-color: white;
  padding: 10px;
  border-radius: 6px;
}

.label {
  .title {
    font-size: 16px;
    color: #000;
    font-weight: 500;
  }
  .key {
    font-size: 12px;
    color: #aaa;
    margin-top: 4px;
  }
}

.link-modes {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.link-item {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
