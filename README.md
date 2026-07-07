# Overlord ML-Agents Integration

Ongoing integration of ML‑Agents for RL testing/development within
the Overlord project.  For the scientific protocol, persona definitions,
reward design, curriculum schedules, and full experimental results,
please refer to the accompanying paper.

## Tech Stack
- **Unity Version:** 6000.3.11f1
- **Python Version:** 3.10.11
- **PyTorch Version:** 2.8.0
- **ML-Agents Unity Package Version:** 4.0.3
- **ML-Agents Python Package Version:** 1.1.0
- **Windows Version:** 10 Pro x64

## Getting Started

After cloning the repository, open the project in Unity (matching the versions listed under **Tech Stack**).
Two scenes are essential; the flow is:

1. `Assets/Scenes/Main.unity` – initialises core Overlord systems, then loads the arena.
2. `Assets/Scenes/ML-Agents-Env.unity` – the actual training/evaluation arena.

### 1. Main scene – enable Arena Mode

- In the **Game Manager** GameObject, find the `Game Manager Singleton` component.
- Tick **Arena Mode** so the procedural generation shortcuts are active.
- The Main scene automatically destroys itself after boot and loads `ML-Agents-Env`.

### 2. ML-Agents-Env – essential flags

On the loaded `ML-Agents-Env` scene, set these two flags to bypass PCG and use the fixed arena:

- **DungeonManager** → `Dungeon Loader` component → **Is Training Mode** = ON.
- **Room** → `Room Bhv` component → **Manual Mode** = ON.

### 3. Key GameObjects and their roles

| GameObject | Component / Setting | What it does |
|------------|---------------------|--------------|
| **Room** | `Room Bhv` | Holds all **spawn points** for enemies and treasures. |
| **ArenaManager** | `ArenaManager` | Central episode controller – see details below. |
| **Player** | `PlayerMLAgent` | The RL agent (reward shaping, ray sensor, etc.). |

#### ArenaManager quick reference

- **Episode Control** – choose whether reaching the exit / killing all enemies ends an episode (death always does).
- **Room Reference** – point to the **Room** GameObject.
- **Training Entities** – list enemy prefabs and their `TopdownEnemySO`s; set min / max count per episode.
- **Treasure Settings** – pick the treasure prefab; set min / max count per episode.
- **Spawn Settings** – fixed or random player spawn (candidates come from the list below).
- **Camera Position** – set the main camera’s location during arena episodes.

#### Player GameObject components

- **Player ML Agent (Script)** – reward weights (hover for tooltips), max steps (not yet implemented).
- **Ray Perception Sensor 2D** – the agent’s 360° vision (32 rays, 3 detectable tags).
- **Behavior Parameters** – set **Model** to a `.onnx` file (inference) or leave empty for training.
- Other components handle movement (`PlayerMovement`), shooting (`PlayerShot`), health (`HealthController`), etc.

### 4. Helpers

- **Missing Script Checker** – flags broken references; useful when debugging, otherwise can be disabled.
- **Training Speed Boost** – speeds up rendering (both in play and training mode). High values (e.g., 20–100) may cause physics glitches, use with care.

## Environment Setup
To ensure reproducibility and avoid version conflicts, follow these steps using [Miniconda](https://docs.anaconda.com/miniconda/):

```bash
# 1. Clone the repository

# 2. Create the environment from the yaml file
conda env create -f environment.yaml

# 3. Activate the environment
conda activate arena_training
```

## Observation & Action Space
### Observation Space
The agent (PlayerMLAgent) receives a hybrid observation:

• A single health‑ratio scalar (currentHealth / maxHealth).
• A 360° spatial representation from a 2D Ray Perception Sensor: 
  32 rays evenly distributed around the agent.

Max ray length: 25 world units.

Detectable tags: Door, Enemy, Treasure.

Since each ray outputs 5 floats (hit fraction, miss indicator, one‑hot tag), the total observation size = 161 real‑valued features.

### Action Space:

- Continuous (2): Movement (Horizontal, Vertical)
- Discrete (1): Shooting (0: Idle, 1: Up, 2: Down, 3: Left, 4: Right)

## Training & Monitoring
To maintain reproducibility, always run training from the project root using the provided configuration:

### Start Training:
```mlagents-learn Config/arena_config.yaml --run-id=Overlord_Alpha_01 --force```

### Monitor with TensorBoard:
```tensorboard --logdir Config/results```

## Overlord's Dependency Chain
The following packages are already within the Unity project, but they are the main packages necessary for Overlord to work seamlessly.

### External Dependencies
| Order | Package Name | Git URL |
| :--- | :--- | :--- |
| 1 | **MyBox** | `https://github.com/Deadcows/mybox.git#1.7.0` |
| 2 | **Reorderable List** | `https://github.com/cfoulston/Unity-Reorderable-List.git#1.0.1` |
| 3 | **Dialogue Module** | `https://github.com/FellowshipOfTheGame/DialogueModule.git#upm` |
