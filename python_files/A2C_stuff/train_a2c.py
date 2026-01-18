import os
import gym

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel

from stable_baselines3 import A2C
from stable_baselines3.common.callbacks import CheckpointCallback
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.vec_env import DummyVecEnv, VecNormalize

from shimmy.openai_gym_compatibility import GymV21CompatibilityV0


class UnityVectorObservationWrapper(gym.Wrapper):
    """
    Naprawia format obserwacji z Unity (z tupli do Box)
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
    unity_env_path = r"../../Build/ML-Agents-Project.exe"  #build po eksportcie z Unity

    models_dir = "models/A2C"
    log_dir = "logs_a2c"

    os.makedirs(models_dir, exist_ok=True)
    os.makedirs(log_dir, exist_ok=True)

    print("1. Uruchamiam środowisko Unity...")
    channel = EngineConfigurationChannel()
    channel.set_configuration_parameters(width=80, height=80, quality_level=1, time_scale=20.0, target_frame_rate=-1)

    unity_env = UnityEnvironment(file_name=unity_env_path, seed=1, no_graphics=True, worker_id=2, side_channels=[channel])

    print("2. Konwertuję środowisko (full rura)...")

    env = UnityToGymWrapper(unity_env, uint8_visual=False, allow_multiple_obs=True)

    env = UnityVectorObservationWrapper(env)

    print("   -> Aplikuję Shimmy...")
    env = GymV21CompatibilityV0(env=env)

    env = Monitor(env, log_dir)

    env = DummyVecEnv([lambda: env])
    env = VecNormalize(env, norm_obs=True, norm_reward=True, clip_obs=10.)

    print("3. Inicjalizuję model A2C...")

    model = A2C(
        "MlpPolicy",
        env,
        verbose=1,
        tensorboard_log=log_dir,

        # --- PARAMETRY A2C ---
        learning_rate=0.0007,
        n_steps=5,
        gamma=0.99,
        gae_lambda=1.0,
        ent_coef=0.01,
        vf_coef=0.5,
        max_grad_norm=0.5,
        rms_prop_eps=1e-5,
        use_rms_prop=True,
        normalize_advantage=False
    )

    checkpoint_callback = CheckpointCallback(save_freq=10000, save_path=models_dir, name_prefix="a2c_model")

    print("4. ROZPOCZYNAM TRENING (A2C)")
    model.learn(total_timesteps=300000, callback=checkpoint_callback)

    print("5. Zapisuję...")
    model.save(f"{models_dir}/a2c_negotiation_final")
    env.save(f"{models_dir}/vec_normalize.pkl")

    env.close()
    print("Środowisko zamknięte.")


if __name__ == '__main__':
    main()