Ongoing integration of ML-Agents for RL testing/development within the Overlord project.

## Tech Stack
- **Unity Version:** 6000.3.11f1
- **Python Version:** 3.10.11
- **PyTorch Version:** 2.8.0
- **ML-Agents Unity Package Version:** 4.0.3
- **ML-Agents Python Package Version:** 1.1.0

## Environment Setup
To ensure reproducibility and avoid version conflicts, follow these steps using [Miniconda](https://docs.anaconda.com/miniconda/):

```bash
# 1. Clone the repository

# 2. Create the environment from the yaml file
conda env create -f environment.yaml

# 3. Activate the environment
conda activate arena_training
```

## Overlord's Dependency Chain
The following packages are already within the Unity project, but they are the main packages necessary for Overlord to work seamlessly.

### External Dependencies
| Order | Package Name | Git URL |
| :--- | :--- | :--- |
| 1 | **MyBox** | `https://github.com/Deadcows/mybox.git#1.7.0` |
| 2 | **Reorderable List** | `https://github.com/cfoulston/Unity-Reorderable-List.git#1.0.1` |
| 3 | **Dialogue Module** | `https://github.com/FellowshipOfTheGame/DialogueModule.git#upm` |
