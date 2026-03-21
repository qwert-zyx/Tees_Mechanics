ChromaHead 技术与开发规范文档 (Technical Spec)
1. 系统架构设计 (System Architecture)

本项目采用“解耦驱动”架构，将输入采集与游戏逻辑完全分离。
1.1 模块关系图

    Input Provider (输入层)：负责采集原始数据（键盘或摄像头）。

    Command Bridge (桥接层)：将原始角度/按键转化为游戏指令（Color_Red, Hit_Trigger）。

    Core Engine (核心引擎)：处理音频同步、音符生成、判定逻辑。

    Feedback System (反馈层)：处理判定线的颜色切换与 UI 表现。

2. 核心技术深度定义
2.1 音频同步：DSP Time 逻辑

痛点：Unity 的 Time.time 受渲染帧率波动影响，会导致音符“瞬移”或对不齐节拍。
方案：使用音频卡硬件时钟 AudioSettings.dspTime。

    主时钟控制：所有音符的 Z轴位置 均由 (TargetTime−CurrentDSPTime) 计算得出。

    预判逻辑：生成器（Spawner）每帧检查谱面列表，若 (Note.HitTime - CurrentDSPTime) <= LeadInTime（预走时间），则从对象池取出音符。

2.2 对象池系统 (Object Pooling)

痛点：音游中 Note 数量巨大，频繁创建/销毁物体会引发 CPU 尖峰导致卡顿（GC Alloc）。
方案：预热池化策略。

    初始化阶段：游戏加载时，预先生成 30个红色Note、30个绿色Note、30个蓝色Note，全部设为 Inactive 状态。

    借用 (Spawn)：当需要音符时，通过 Queue<GameObject> 弹出第一个可用物体，重置坐标并 SetActive(true)。

    回收 (Despawn)：音符被击中或飞出屏幕底端时，立即调用回池函数，设为 Inactive。

2.3 判定线状态机 (Judgment State Machine)

判定线通过一个简单的状态机（FSM）管理材质颜色：

    Black (Default)：无操作。

    Success (Green)：满足 Distance < Threshold && Color == Target。触发后启动 0.15s 计时。

    Failure (Red)：满足 Distance > Threshold || Color != Target。触发后启动 0.15s 计时。

    视觉平滑：使用 Color.Lerp（线性插值）让颜色从绿/红平滑过渡回黑色，而非生硬跳变。

3. 体感动作识别算法 (CV Algorithm Logic)
3.1 头部偏航角 (Yaw) 状态判定

通过 MediaPipe 得到的姿态角不是瞬时的触发，而是持续的状态：

    区间过滤：

        Angle < -20° → 状态：Active_Red

        -10° < Angle < 10° → 状态：Active_Green

        Angle > 20° → 状态：Active_Blue

    滞后校准 (Hysteresis)：为了防止在 20° 边缘左右摆动导致颜色反复闪烁，设置 ±2° 的缓冲区。

3.2 点头 (Pitch) 触发算法

不同于旋转，点头是脉冲信号。

    角速度检测：记录上一帧角度 A1​ 与本帧角度 A2​。

    阈值判断：若 DeltaTimeA2​−A1​​>Threshold（即向下摆动的速度够快），且头部当前处于向下倾斜状态，则触发一次 Action_Hit。

    冷却机制：触发后 200ms 内屏蔽所有点头信号，防止“低头”和“抬起”被识别成两次点击。

4. 数据结构规范 (Data Structures)
4.1 谱面文件格式 (JSON Example)
JSON

{
  "songName": "SampleTrack",
  "bpm": 120,
  "offset": 0.05,
  "notes": [
    { "time": 1.5, "type": 0 },  // 0: Red, 1: Green, 2: Blue
    { "time": 3.0, "type": 1 },
    { "time": 4.5, "type": 2 }
  ]
}

5. 阶段性交付清单 (Deliverables)
P1：反馈验证版

    [ ] 实现判定线 Shader 控制脚本（支持 SetColor 方法）。

    [ ] 完成键盘模拟击打反馈，测试回弹时间是否符合视觉直觉。

P2：同步生成版

    [ ] 编写基于 dspTime 的 Note 移动脚本。

    [ ] 完成对象池管理器（ObjectPooler）。

    [ ] 验证：在 60FPS 和 30FPS 下，音符到达终点的时间完全一致。

P3：逻辑闭环版

    [ ] 实现判定处理器（判断颜色匹配和时间偏差）。

    [ ] 接入 Combo 计数器和 UI 文字反馈。

P4：体感映射版

    [ ] 接入 MediaPipe 数据流。

    [ ] 调试动作阈值：确保“点头”的误触发率低于 5%。

P5：综合调优版

    [ ] 增加判定线在 Perfect 时的“发光”特效。

    [ ] 增加画面震动（Screen Shake）系统。
