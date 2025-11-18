import os
import gym # Stary gym dla Unity
import gymnasium # Nowy gym dla Stable Baselines

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
from stable_baselines3 import DQN
from stable_baselines3.common.callbacks import CheckpointCallback
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.vec_env import DummyVecEnv, VecNormalize

# --- NOWY IMPORT: PRZEJŚCIÓWKA (BRIDGE) ---
# To zamienia stary gym na nowy gymnasium
from shimmy.openai_gym_compatibility import GymV21CompatibilityV0


class UnityVectorObservationWrapper(gym.Wrapper):
    """
    Ten wrapper naprawia błąd "NotImplementedError: Tuple(Box...".
    Wyjmuje wektor obserwacji z Krotki (Tuple), którą zwraca Unity.
    """
    def __init__(self, env):
        super().__init__(env)
        # Bierzemy pierwszy element z definicji przestrzeni (to jest nasz Box(6,))
        self.observation_space = env.observation_space[0]

    def reset(self, **kwargs):
        obs = self.env.reset(**kwargs)
        # Jeśli obs to krotka/lista, weź pierwszy element
        if isinstance(obs, (tuple, list)):
            return obs[0]
        return obs

    def step(self, action):
        obs, reward, done, info = self.env.step(action)
        # Jeśli obs to krotka/lista, weź pierwszy element
        if isinstance(obs, (tuple, list)):
            obs = obs[0]
        return obs, reward, done, info

def main():
    # --- KONFIGURACJA ŚCIEŻEK ---
    # Upewnij się, że nazwa pliku zgadza się z Twoim buildem!
    # Jeśli jesteś na Windowsie, pamiętaj o .exe
    unity_env_path = r"D:\UnityProjects\negotiation_agents\Build\ML-Agents-Project.exe"

    # Folder na zapisywanie modeli i logów
    models_dir = "models/DQN"
    log_dir = "logs_dqn"

    os.makedirs(models_dir, exist_ok=True)
    os.makedirs(log_dir, exist_ok=True)

    print("1. Uruchamiam środowisko Unity...")
    # no_graphics=True -> przyspiesza trening (nie renderuje okna gry)
    # worker_id -> unikalne ID, pozwala uruchomić kilka treningów naraz (zostaw 1)
    unity_env = UnityEnvironment(file_name=unity_env_path, seed=1, no_graphics=False, worker_id=1)

    print("2. Konwertuję środowisko do formatu Gym...")
    # To jest magiczna linijka, która tłumaczy Unity na język zrozumiały dla DQN
    env = UnityToGymWrapper(
        unity_env,
        uint8_visual=False,  # False, bo używamy liczb (Vector Obs), a nie kamery
        allow_multiple_obs=True
    )

    env = UnityVectorObservationWrapper(env)
    env = GymV21CompatibilityV0(env=env)

    env = Monitor(env, log_dir)

    env = DummyVecEnv([lambda: env])
    env = VecNormalize(env, norm_obs=True, norm_reward=True, clip_obs=10.)

    print("3. Inicjalizuję Jacka (DQN Optimized)...")
    # MlpPolicy = Multi Layer Perceptron (sieć neuronowa dla danych liczbowych)
    model = DQN(
        "MlpPolicy",
        env,
        verbose=1,
        tensorboard_log=log_dir,
        learning_rate=0.0001,
        buffer_size=100000,  # Pamięć doświadczeń
        learning_starts=1000,  # Ile losowych kroków zrobić przed nauką
        batch_size=64,
        gamma=0.99,  # Jak bardzo zależy nam na przyszłych nagrodach
        exploration_fraction=0.3,  # Jak długo agent ma "błądzić" (eksplorować)
        target_update_interval=1000
    )

    # Callback, żeby zapisywać model co np. 10 000 kroków (bezpiecznik)
    checkpoint_callback = CheckpointCallback(save_freq=10000, save_path=models_dir, name_prefix="dqn_model")

    print("4. ROZPOCZYNAM TRENING! 🚀")
    # Trenujemy przez 100 000 kroków (możesz zmienić)
    model.learn(total_timesteps=100000, callback=checkpoint_callback)

    print("5. Trening zakończony. Zapisuję finalny model.")
    model.save(f"{models_dir}/dqn_negotiation_final")

    env.close()
    print("Środowisko zamknięte.")


if __name__ == '__main__':
    main()