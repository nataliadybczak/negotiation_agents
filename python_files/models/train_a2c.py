import os
import gym  # Stary gym dla Unity
import gymnasium  # Nowy gym dla Stable Baselines

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper

# --- IMPORTUJEMY A2C ---
from stable_baselines3 import A2C
from stable_baselines3.common.callbacks import CheckpointCallback
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.vec_env import DummyVecEnv, VecNormalize

# Przejściówka Gym -> Gymnasium
from shimmy.openai_gym_compatibility import GymV21CompatibilityV0


# --- WRAPPER (Ten sam co w DQN) ---
class UnityVectorObservationWrapper(gym.Wrapper):
    """
    Naprawia format obserwacji z Unity (Tuple -> Box)
    """

    def __init__(self, env):
        super().__init__(env)
        self.observation_space = env.observation_space[0]

    def reset(self, **kwargs):
        obs = self.env.reset(**kwargs)
        if isinstance(obs, (tuple, list)): return obs[0]
        return obs

    def step(self, action):
        obs, reward, done, info = self.env.step(action)
        if isinstance(obs, (tuple, list)): obs = obs[0]
        return obs, reward, done, info


# -----------------------------------------

def main():
    # 1. KONFIGURACJA ŚCIEŻKI (Twoja ścieżka do Builda)
    unity_env_path = r"D:\UnityProjects\negotiation_agents\Build\ML-Agents-Project.exe"

    models_dir = "models/A2C"
    log_dir = "logs_a2c"

    os.makedirs(models_dir, exist_ok=True)
    os.makedirs(log_dir, exist_ok=True)

    print("1. Uruchamiam środowisko Unity...")
    unity_env = UnityEnvironment(file_name=unity_env_path, seed=1, no_graphics=False, worker_id=2)
    # Zmieniłem worker_id na 2, żeby nie gryzł się z DQN, jakbyś odpaliła oba naraz :)

    print("2. Konwertuję środowisko (Pełna rura)...")

    # A. Unity -> Stary Gym
    env = UnityToGymWrapper(unity_env, uint8_visual=False, allow_multiple_obs=True)

    # B. Naprawa Tuple
    env = UnityVectorObservationWrapper(env)

    # C. Shimmy (Stary Gym -> Nowy Gymnasium)
    print("   -> Aplikuję Shimmy...")
    env = GymV21CompatibilityV0(env=env)

    # D. Monitor (Logowanie nagród)
    env = Monitor(env, log_dir)

    # E. Normalizacja (A2C też tego potrzebuje!)
    env = DummyVecEnv([lambda: env])
    env = VecNormalize(env, norm_obs=True, norm_reward=True, clip_obs=10.)

    print("3. Inicjalizuję model A2C...")

    model = A2C(
        "MlpPolicy",
        env,
        verbose=1,
        tensorboard_log=log_dir,

        # --- PARAMETRY A2C ---
        learning_rate=0.0007,  # A2C lubi uczyć się szybciej niż DQN
        n_steps=5,  # Aktualizuje mózg co 5 kroków (bardzo często!)
        gamma=0.99,  # Dalekowzroczność
        gae_lambda=1.0,
        ent_coef=0.01,  # Entropia (Ciekawość) - żeby nie utknął w miejscu
        vf_coef=0.5,  # Waga oceny sytuacji
        max_grad_norm=0.5,
        rms_prop_eps=1e-5,
        use_rms_prop=True,  # Specyficzny optymalizator dla A2C
        normalize_advantage=False
    )

    checkpoint_callback = CheckpointCallback(save_freq=10000, save_path=models_dir, name_prefix="a2c_model")

    print("4. ROZPOCZYNAM TRENING (A2C)! 🚀")
    # 200 000 kroków dla A2C to chwila moment
    model.learn(total_timesteps=200000, callback=checkpoint_callback)

    print("5. Zapisuję...")
    model.save(f"{models_dir}/a2c_negotiation_final")
    env.save(f"{models_dir}/vec_normalize.pkl")

    env.close()
    print("Środowisko zamknięte.")


if __name__ == '__main__':
    main()