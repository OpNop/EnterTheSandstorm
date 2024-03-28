using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpNop.EnterTheSandstorm
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger<Module>();
        private IWavePlayer _soundClip;
        private VolumeSampleProvider _volumeSampler;
        private LoopingAudioStream _audioStream;
        private double _timeSinceUpdate;
        private int LastMap = 0;
        private bool _dryTopTriggered = false;
        //Map ID's
        private readonly int DryTop = 988;
        private readonly int Oasis = 1210;

        //Settings
        private SettingEntry<float> _masterVolume;
        private SettingEntry<bool> _loop;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        {
            _masterVolume = settings.DefineSetting("MasterVolume.", 50.0f, () => "Master Volume", () => "Getting too dusty for ya?");
            _loop = settings.DefineSetting("Loop", true, () => "Loop in Dry Top during Sandstorm", () => "Do you want the song to loop during the sandstorm event, or only at the start?");
        }

        protected override void Initialize()
        {

        }

        protected override async Task LoadAsync()
        {
            
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            var stream = ContentsManager.GetFileStream("sandstorm.mp3");
            _audioStream = new LoopingAudioStream(new Mp3FileReader(stream));
            _volumeSampler = new VolumeSampleProvider(_audioStream.ToSampleProvider());

            _soundClip = new WaveOutEvent();
            _soundClip.Init(_volumeSampler);

            //Start playing the music at 0 volume
            _volumeSampler.Volume = 0f;

            //Catch when the games is closed and started to bring on the music
            GameService.GameIntegration.Gw2Instance.Gw2Closed += GameIntegration_Gw2Closed;
            GameService.GameIntegration.Gw2Instance.Gw2Started += GameIntegration_Gw2Started;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void GameIntegration_Gw2Started(object sender, EventArgs e)
        {

        }

        private void GameIntegration_Gw2Closed(object sender, EventArgs e)
        {
            _soundClip.Stop();
        }

        private async void UpdateVolume(GameTime gameTime, int CurrentMap)
        {
            // Expensive to set the volume
            if (_timeSinceUpdate < 200)
            {
                _timeSinceUpdate += gameTime.ElapsedGameTime.TotalMilliseconds;
                return;
            }

            _timeSinceUpdate = 0;

            //REMEMBER: Blish crossed his wires, Z=Y and Y=Z
            //For BlishOS use Z
            var playerPosition = GameService.Gw2Mumble.PlayerCharacter.Position;
            float volume;

            //For Dry Top
            if (DryTop == CurrentMap)
            {
                if (DateTime.UtcNow.Minute > 39 && DateTime.UtcNow.Minute <= 59)
                {
                    if (_dryTopTriggered == false)
                    {
                        _soundClip.Stop();
                        _audioStream.Seek(0, SeekOrigin.Begin);
                        _soundClip.Play();
                        _dryTopTriggered = true;
                    }

                    if (DateTime.UtcNow.Minute == 59)
                    {
                        volume = Map(DateTime.UtcNow.Second, 44, 59, (_masterVolume.Value / 100), 0);
                    }
                    else
                    {
                        volume = (_masterVolume.Value / 100);
                    }
                }
                else
                {
                    volume = 0f;
                    _soundClip.Stop();
                    _dryTopTriggered = false;
                }
            }
            //For Crystal Oasis
            //else if (Oasis == CurrentMap)
            //{
            //    Vector3 Casino = new Vector3() { X = -905.6f, Y = -287.5f, Z = 12.2f };
            //    Vector3 Lilly = new Vector3() { X = -1016.3f, Y = -444f, Z = 1.3f };

            //    float cDistance = Vector3.Distance(playerPosition, Casino);
            //    float lDistance = Vector3.Distance(playerPosition, Lilly);
            //    var distance = Math.Min(cDistance, lDistance);
                
            //    volume = Map(distance, 15, 0, 0, (_masterVolume.Value / 100));
            //}
            //Otherwise
            else
            {
                volume = 0f;
            }

            //Lets not get crazy here, keep it between 0 and Max Volume
            volume = Clamp(volume, 0f, (_masterVolume.Value / 100));

            //Set the volume
            _volumeSampler.Volume = volume;
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private static float Map(float value, float fromLow, float fromHigh, float toLow, float toHigh)
        {
            return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
        }

        protected override async void Update(GameTime gameTime)
        {
            int CurrentMap = GameService.Gw2Mumble.CurrentMap.Id;

            //Check for map change
            if (LastMap != CurrentMap)
            {
                LastMap = CurrentMap;

                //Check if we are in a map we care about
                if (CurrentMap == DryTop || CurrentMap == Oasis)
                {
                    if (_soundClip.PlaybackState != PlaybackState.Playing)
                    {
                        _audioStream.Seek(0, SeekOrigin.Begin);
                        _soundClip.Play();
                    }
                }
                else
                {
                    _soundClip.Stop();
                }
            }

            UpdateVolume(gameTime, CurrentMap);
        }

        protected override void Unload()
        {
            // Unload
            _soundClip?.Stop();
            _soundClip?.Dispose();
        }
    }
}