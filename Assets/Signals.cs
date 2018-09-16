using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using KmHelper;
using System.Security.Cryptography;
using System.Text;

public class Signals : MonoBehaviour
{
    private static int idCounter = 1;
    private readonly int moduleId = 0;
    private readonly Logger logger;

    private enum ModuleStateE {
        START,
        AWAKE,
        ACTIVE,
        DISARMED
    }

    private ModuleStateE moduleState;

    // BINDS
    public KMBombModule BombModule;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMGameInfo GameInfo;

    public GameObject scopeObj;

    public GameObject S1Obj;
    public GameObject S2Obj;
    public GameObject S3Obj;
    public GameObject CSObj;
    public GameObject BObj;

    public TextAsset SignalTextureOffsetsJson;
    public TextAsset SolutionSignalsNoStrikesJson;
    public TextAsset SolutionSignalsOneStrikeJson;
    public TextAsset SolutionSignalsTwoStrikesJson;
    // BINDS END

    private KMSelectable S1Selectable;
    private KMSelectable S2Selectable;
    private KMSelectable S3Selectable;
    private KMSelectable CSSelectable;
    private KMSelectable BSelectable;

    private Switch S1;
    private Switch S2;
    private Switch S3;
    private Selector CS;
    private Button B;

    private SwitchStateMapping S1Mapping;
    private SwitchStateMapping S2Mapping;
    private SwitchStateMapping S3Mapping;
    private SwitchMapping switchMapping;
    private Scope scope;

    private Signal inputSignal;
    private Signal generatorSignal;
    private SolutionSignals solutionSignalsNoStrikes;
    private SolutionSignals solutionSignalsOneStrike;
    private SolutionSignals solutionSignalsTwoStrikes;

    private class Logger
    {
        private readonly int moduleId;
        private readonly string moduleName;

        public Logger(string _moduleName, int _moduleId)
        {
            moduleId = _moduleId;
            moduleName = _moduleName;
        }

        public void DEBUG(string message)
        {
            Debug.LogFormat("[{0} #{1}] {2}", moduleName, moduleId, message);
        }
    }

    private static GameObject GetChildWithName(GameObject parentObj, string name)
    {
        Transform parentTrans = parentObj.transform;
        Transform childTrans = parentTrans.Find(name);
        if (childTrans != null)
            return childTrans.gameObject;

        throw new System.ApplicationException(string.Format("GetChildWithName: child not found: {0} (parent: {1})", name, parentObj.GetType().Name));
    }

    private struct Triple
    {
        public object OBJ1;
        public object OBJ2;
        public object OBJ3;

        public Triple(object _OBJ1, object _OBJ2, object _OBJ3)
        {
            OBJ1 = _OBJ1;
            OBJ2 = _OBJ2;
            OBJ3 = _OBJ3;
        }
    }

    private class SwitchMapping
    {
        private readonly int mapping;

        private readonly string[] mappingStrings = {
                "S1->C1,S2->C2,S3->C3",
                "S1->C1,S2->C3,S3->C2",
                "S1->C2,S2->C1,S3->C3",
                "S1->C2,S2->C3,S3->C1",
                "S1->C3,S2->C1,S3->C2",
                "S1->C3,S2->C2,S3->C1"
        };

        public SwitchMapping()
        {
            mapping = Random.Range(0, 5);
        }

        public Triple Map(Triple input)
        {
            switch (mapping)
            {
                case 0: return new Triple(input.OBJ1, input.OBJ2, input.OBJ3);
                case 1: return new Triple(input.OBJ1, input.OBJ3, input.OBJ2);
                case 2: return new Triple(input.OBJ2, input.OBJ1, input.OBJ3);
                case 3: return new Triple(input.OBJ2, input.OBJ3, input.OBJ1);
                case 4: return new Triple(input.OBJ3, input.OBJ1, input.OBJ2);
                case 5: return new Triple(input.OBJ3, input.OBJ2, input.OBJ1);
                default: throw new System.ApplicationException();
            }
        }

        public override string ToString()
        {
            return mappingStrings[mapping];
        }
    }

    private class SwitchStateMapping
    {
        private readonly Signal.CoefficientE UpMapped;
        private readonly Signal.CoefficientE DownMapped;
        private readonly Signal.CoefficientE CenterMapped;

        public SwitchStateMapping()
        {
            UpMapped = Signal.CoefficientE.POSITIVE;
            DownMapped = Signal.CoefficientE.NEGATIVE;
            CenterMapped = Signal.CoefficientE.ZERO;

            Signal.CoefficientE tmp;
            int j;
            j = Random.Range(0, 2);
            if (j == 1)
            {
                tmp = CenterMapped;
                CenterMapped = DownMapped;
                DownMapped = tmp;
            }
            else if (j == 0)
            {
                tmp = CenterMapped;
                CenterMapped = UpMapped;
                UpMapped = tmp;
            }

            j = Random.Range(0, 1);
            if (j == 0)
            {
                tmp = DownMapped;
                DownMapped = UpMapped;
                UpMapped = tmp;
            }
        }

        public Signal.CoefficientE Map(Switch.StateE switchState)
        {
            switch (switchState)
            {
                case Switch.StateE.UP:
                    return UpMapped;
                case Switch.StateE.DOWN:
                    return DownMapped;
                case Switch.StateE.CENTER_NEXT_DOWN:
                case Switch.StateE.CENTER_NEXT_UP:
                    return CenterMapped;
                default:
                    throw new System.ApplicationException();
            }
        }

        public override string ToString()
        {
            return string.Format("UP->{0},DOWN->{1},CENTER->{2}", UpMapped.ToString(), DownMapped.ToString(), CenterMapped.ToString());
        }
    }

    private class Signal
    {
        public enum CoefficientE
        {
            ZERO = 0,
            POSITIVE = 1,
            NEGATIVE = 2
        }

        [JsonProperty] private CoefficientE[] coefficients = new CoefficientE[3];
        
        public Signal(CoefficientE C1, CoefficientE C2, CoefficientE C3)
        {
            coefficients[0] = C1;
            coefficients[1] = C2;
            coefficients[2] = C3;
        }

        public Signal()
        {
            for (int i = 0; i < coefficients.Length; i++)
            {
                switch (Random.Range(0, 2))
                {
                    case 0: coefficients[i] = CoefficientE.ZERO; break;
                    case 1: coefficients[i] = CoefficientE.POSITIVE; break;
                    case 2: coefficients[i] = CoefficientE.NEGATIVE; break;
                }
            }
        }

        public void Set(CoefficientE C1, CoefficientE C2, CoefficientE C3)
        {
            coefficients[0] = C1;
            coefficients[1] = C2;
            coefficients[2] = C3;
        }

        public static bool operator ==(Signal lhs, Signal rhs)
        {
            return lhs.coefficients[0] == rhs.coefficients[0]
                && lhs.coefficients[1] == rhs.coefficients[1]
                && lhs.coefficients[2] == rhs.coefficients[2];
        }

        public static bool operator !=(Signal lhs, Signal rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object rhs)
        {
            if (rhs.GetType() != this.GetType())
                return false;

            return this == (Signal)rhs;
        }

        public override int GetHashCode()
        {
            string combined = coefficients[0].ToString() + coefficients[1].ToString() + coefficients[2].ToString();
            return combined.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("(C1={0},C2={1},C3={2})", coefficients[0], coefficients[1], coefficients[2]);
        }
    }

    private class Button
    {
        private readonly MeshRenderer rendererReleased;
        private readonly MeshRenderer rendererPressed;

        public Button(GameObject obj)
        {
            rendererReleased = GetChildWithName(obj, "rendererReleased").GetComponent<MeshRenderer>();
            rendererPressed = GetChildWithName(obj, "rendererPressed").GetComponent<MeshRenderer>();
            Released();
        }

        public void Pressed()
        {
            rendererReleased.gameObject.SetActive(false);
            rendererPressed.gameObject.SetActive(true);
        }

        public void Released()
        {
            rendererReleased.gameObject.SetActive(true);
            rendererPressed.gameObject.SetActive(false);
        }
    }

    private class Selector
    {
        private readonly MeshRenderer rendererLeft;
        private readonly MeshRenderer rendererRight;

        public enum StateE
        {
            LEFT,
            RIGHT
        };
        private StateE _state;
        public virtual StateE State { set { _state = value; UpdateRenderers(); } get { return _state; } }

        public Selector(GameObject obj, StateE initialState)
        {
            rendererLeft = GetChildWithName(obj, "rendererLeft").GetComponent<MeshRenderer>();
            rendererRight = GetChildWithName(obj, "rendererRight").GetComponent<MeshRenderer>();
            State = initialState;
        }

        public StateE Clicked()
        {
            if (State == StateE.LEFT)
                State = StateE.RIGHT;
            else
                State = StateE.LEFT;
            return State;
        }

        private void UpdateRenderers()
        {
            switch (State)
            {
                case StateE.LEFT:
                    rendererLeft.gameObject.SetActive(true);
                    rendererRight.gameObject.SetActive(false);
                    break;
                case StateE.RIGHT:
                    rendererLeft.gameObject.SetActive(false);
                    rendererRight.gameObject.SetActive(true);
                    break;
            }
        }
    }

    private class Switch
    {
        private readonly MeshRenderer rendererNorth;
        private readonly MeshRenderer rendererSouth;
        private readonly MeshRenderer rendererCenter;

        public enum StateE
        {
            UP,
            CENTER_NEXT_DOWN,
            DOWN,
            CENTER_NEXT_UP
        };
        private StateE _state;
        public StateE State { set { _state = value; UpdateRenderers(); } get { return _state;  } }

        public Switch(GameObject obj, StateE initialState)
        {
            rendererNorth = GetChildWithName(obj, "rendererNorth").GetComponent<MeshRenderer>();
            rendererSouth = GetChildWithName(obj, "rendererSouth").GetComponent<MeshRenderer>();
            rendererCenter = GetChildWithName(obj, "rendererCenter").GetComponent<MeshRenderer>();
            State = initialState;
        }

        public StateE Clicked()
        {
            switch (State)
            {
                case StateE.UP:
                    State = StateE.CENTER_NEXT_DOWN;
                    break;
                case StateE.CENTER_NEXT_DOWN:
                    State = StateE.DOWN;
                    break;
                case StateE.DOWN:
                    State = StateE.CENTER_NEXT_UP;
                    break;
                case StateE.CENTER_NEXT_UP:
                    State = StateE.UP;
                    break;
            }
            return State;
        }

        private void UpdateRenderers()
        {
            switch (State)
            {
                case StateE.UP:
                    rendererNorth.gameObject.SetActive(true);
                    rendererCenter.gameObject.SetActive(false);
                    rendererSouth.gameObject.SetActive(false);
                    break;
                case StateE.DOWN:
                    rendererNorth.gameObject.SetActive(false);
                    rendererCenter.gameObject.SetActive(false);
                    rendererSouth.gameObject.SetActive(true);
                    break;
                case StateE.CENTER_NEXT_DOWN:
                case StateE.CENTER_NEXT_UP:
                    rendererNorth.gameObject.SetActive(false);
                    rendererCenter.gameObject.SetActive(true);
                    rendererSouth.gameObject.SetActive(false);
                    break;
            }
        }
    }

    private class Scope
    {
        public enum ChannelE { A, B }
        public ChannelE Channel { set; get; }
        public Signal SignalA { set; get; }
        public Signal SignalB { set; get; }
        public bool Day { set; get; }

        private readonly MeshRenderer rendererBackgroundDayA;
        private readonly MeshRenderer rendererBackgroundDayB;
        private readonly MeshRenderer rendererBackgroundNight;
        private readonly MeshRenderer rendererSignalDay;
        private readonly MeshRenderer rendererSignalNight;

        private readonly Dictionary<Signal, Vector2> signalTextureOffsets = new Dictionary<Signal, Vector2>();
        private struct JsonFormat
        {
            [JsonProperty] public Signal signal;
            [JsonProperty] public Vector2 offset;

            public JsonFormat(Signal _signal, Vector2 _offset)
            {
                signal = _signal;
                offset = _offset;
            }
        };

        public Scope(GameObject obj, ChannelE _channel, Signal _signalA, Signal _signalB, bool _day, TextAsset signalTextureOffsetsJson)
        {
            List<JsonFormat> deserialized = JsonConvert.DeserializeObject<List<JsonFormat>>(signalTextureOffsetsJson.text);
            foreach (JsonFormat element in deserialized)
                signalTextureOffsets.Add(element.signal, element.offset);

            rendererBackgroundDayA = GetChildWithName(obj, "rendererBackgroundDayA").GetComponent<MeshRenderer>();
            rendererBackgroundDayB = GetChildWithName(obj, "rendererBackgroundDayB").GetComponent<MeshRenderer>();
            rendererBackgroundNight = GetChildWithName(obj, "rendererBackgroundNight").GetComponent<MeshRenderer>();
            rendererSignalDay = GetChildWithName(obj, "rendererSignalDay").GetComponent<MeshRenderer>();
            rendererSignalNight = GetChildWithName(obj, "rendererSignalNight").GetComponent<MeshRenderer>();

            SignalA = _signalA;
            SignalB = _signalB;
            Day = _day;
            Channel = _channel;

            Update();
        }

        public void Update()
        {
            Vector2 offset;
            if (Channel == ChannelE.A)
            {
                if (!signalTextureOffsets.TryGetValue(SignalA, out offset))
                    throw new System.ApplicationException(string.Format("Scope.Update: key not found in signalTextureOffsets dictionary: {0}", SignalA.ToString()));
            }
            else
            {
                if (!signalTextureOffsets.TryGetValue(SignalB, out offset))
                    throw new System.ApplicationException(string.Format("Scope.Update: key not found in signalTextureOffsets dictionary: {0}", SignalB.ToString()));
            }

            if (Day)
            {
                rendererSignalDay.material.SetTextureOffset("_MainTex", offset);

                if (Channel == ChannelE.A)
                {
                    rendererBackgroundDayA.gameObject.SetActive(true);
                    rendererBackgroundDayB.gameObject.SetActive(false);
                }
                else
                {
                    rendererBackgroundDayA.gameObject.SetActive(false);
                    rendererBackgroundDayB.gameObject.SetActive(true);
                }
                rendererBackgroundNight.gameObject.SetActive(false);
                rendererSignalDay.gameObject.SetActive(true);
                rendererSignalNight.gameObject.SetActive(false);
            }
            else
            {
                rendererSignalNight.material.SetTextureOffset("_MainTex", offset);

                rendererBackgroundDayA.gameObject.SetActive(false);
                rendererBackgroundDayB.gameObject.SetActive(false);
                rendererBackgroundNight.gameObject.SetActive(true);
                rendererSignalDay.gameObject.SetActive(false);
                rendererSignalNight.gameObject.SetActive(true);
            }
        }
    }

    private class SolutionSignals
    {
        private readonly Dictionary<Signal, Signal> solutionSignals = new Dictionary<Signal, Signal>();

        private struct JsonFormat
        {
            [JsonProperty] public Signal signal;
            [JsonProperty] public Signal solution;

            public JsonFormat(Signal _signal, Signal _solution)
            {
                signal = _signal;
                solution = _solution;
            }
        };

        public SolutionSignals(TextAsset signalResponsesJson)
        {
            List<JsonFormat> deserialized = JsonConvert.DeserializeObject<List<JsonFormat>>(signalResponsesJson.text);
            foreach (JsonFormat element in deserialized)
                solutionSignals.Add(element.signal, element.solution);
        }

        public Signal Get(Signal input)
        {
            Signal rv;
            if(!solutionSignals.TryGetValue(input, out rv))
                throw new System.ApplicationException(string.Format("SolutionSignals.Get: key not found in solutionSignals dictionary: {0}", input.ToString()));

            return rv;
        }
    }

    public Signals()
    {
        moduleId = idCounter++;
        logger = new Logger("Signals", moduleId);
    }

    // bomb generation (loading screen)
    public void Start()
    {
        moduleState = ModuleStateE.START;
        logger.DEBUG(string.Format("STATE:{0}", moduleState.ToString()));

        string seedStr = string.Format("{0}{1}", BombInfo.GetSerialNumber(), moduleId);
        byte[] seedBytes = new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(seedStr));
        int seedInt = System.BitConverter.ToInt32(seedBytes, 0);
        Random.InitState(seedInt);
        logger.DEBUG(string.Format("Seed: {0}", seedInt));

        BombModule.OnActivate += Activate;

        S1 = new Switch(S1Obj, Switch.StateE.DOWN);
        S1Selectable = S1Obj.GetComponent<KMSelectable>();
        S1Selectable.OnInteract += delegate () { HandleS1Interact(); return false; };
        S2 = new Switch(S2Obj, Switch.StateE.DOWN);
        S2Selectable = S2Obj.GetComponent<KMSelectable>();
        S2Selectable.OnInteract += delegate () { HandleS2Interact(); return false; };
        S3 = new Switch(S3Obj, Switch.StateE.DOWN);
        S3Selectable = S3Obj.GetComponent<KMSelectable>();
        S3Selectable.OnInteract += delegate () { HandleS3Interact(); return false; };

        CS = new Selector(CSObj, Selector.StateE.LEFT);
        CSSelectable = CSObj.GetComponent<KMSelectable>();
        CSSelectable.OnInteract += delegate () { HandleCSInteract(); return false; };

        B = new Button(BObj);
        BSelectable = BObj.GetComponent<KMSelectable>();
        BSelectable.OnInteract += delegate () { HandleBInteract(); return false; };
        BSelectable.OnInteractEnded += delegate () { HandleBInteractEnded(); };

        S1Mapping = new SwitchStateMapping();
        logger.DEBUG(string.Format("S1 Mapping: {0}", S1Mapping.ToString()));
        S2Mapping = new SwitchStateMapping();
        logger.DEBUG(string.Format("S2 Mapping: {0}", S2Mapping.ToString()));
        S3Mapping = new SwitchStateMapping();
        logger.DEBUG(string.Format("S3 Mapping: {0}", S3Mapping.ToString()));

        switchMapping = new SwitchMapping();
        logger.DEBUG(string.Format("SwitchMapping: {0}", switchMapping.ToString()));

        inputSignal = new Signal();
        logger.DEBUG(string.Format("INPUT: {0}", inputSignal.ToString()));

        solutionSignalsNoStrikes = new SolutionSignals(SolutionSignalsNoStrikesJson);
        solutionSignalsOneStrike = new SolutionSignals(SolutionSignalsOneStrikeJson);
        solutionSignalsTwoStrikes = new SolutionSignals(SolutionSignalsTwoStrikesJson);

        Triple switchesMapped = switchMapping.Map(new Triple(S1Mapping.Map(S1.State), S2Mapping.Map(S2.State), S3Mapping.Map(S3.State)));
        generatorSignal = new Signal(
            (Signal.CoefficientE)switchesMapped.OBJ1,
            (Signal.CoefficientE)switchesMapped.OBJ2,
            (Signal.CoefficientE)switchesMapped.OBJ3);
        logger.DEBUG(string.Format("GENERATOR: {0}", generatorSignal.ToString()));

        scope = new Scope(scopeObj, (CS.State == Selector.StateE.LEFT) ? Scope.ChannelE.A : Scope.ChannelE.B, inputSignal, generatorSignal, false, SignalTextureOffsetsJson);
        GameInfo.OnLightsChange += HandleLightChange;
    }

    // room shown, lights not yet on
    public void Awake()
    {
        moduleState = ModuleStateE.AWAKE;
        logger.DEBUG(string.Format("STATE:{0}", moduleState.ToString()));
    }

    // timer starts, light is on
    public void Activate()
    {
        moduleState = ModuleStateE.ACTIVE;
        logger.DEBUG(string.Format("STATE:{0}", moduleState.ToString()));
    }

    // Update is called once per frame
    // public void Update() { }

    private void HandleS1Interact()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, S1Selectable.transform);
        S1.Clicked();
        logger.DEBUG(string.Format("S1->{0}", S1.State.ToString()));
        Triple switchesMapped = switchMapping.Map(new Triple(S1Mapping.Map(S1.State), S2Mapping.Map(S2.State), S3Mapping.Map(S3.State)));
        generatorSignal.Set(
            (Signal.CoefficientE)switchesMapped.OBJ1,
            (Signal.CoefficientE)switchesMapped.OBJ2,
            (Signal.CoefficientE)switchesMapped.OBJ3);
        logger.DEBUG(string.Format("GENERATOR: {0}", generatorSignal.ToString()));
        scope.SignalB = generatorSignal;
        scope.Update();
    }

    private void HandleS2Interact()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, S2Selectable.transform);
        S2.Clicked();
        logger.DEBUG(string.Format("S2->{0}", S2.State.ToString()));
        Triple switchesMapped = switchMapping.Map(new Triple(S1Mapping.Map(S1.State), S2Mapping.Map(S2.State), S3Mapping.Map(S3.State)));
        generatorSignal.Set(
            (Signal.CoefficientE)switchesMapped.OBJ1,
            (Signal.CoefficientE)switchesMapped.OBJ2,
            (Signal.CoefficientE)switchesMapped.OBJ3);
        logger.DEBUG(string.Format("GENERATOR: {0}", generatorSignal.ToString()));
        scope.SignalB = generatorSignal;
        scope.Update();
    }

    private void HandleS3Interact()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, S3Selectable.transform);
        S3.Clicked();
        logger.DEBUG(string.Format("S3->{0}", S3.State.ToString()));
        Triple switchesMapped = switchMapping.Map(new Triple(S1Mapping.Map(S1.State), S2Mapping.Map(S2.State), S3Mapping.Map(S3.State)));
        generatorSignal.Set(
            (Signal.CoefficientE)switchesMapped.OBJ1,
            (Signal.CoefficientE)switchesMapped.OBJ2,
            (Signal.CoefficientE)switchesMapped.OBJ3);
        logger.DEBUG(string.Format("GENERATOR: {0}", generatorSignal.ToString()));
        scope.SignalB = generatorSignal;
        scope.Update();
    }

    private void HandleCSInteract()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CSSelectable.transform);
        CSSelectable.AddInteractionPunch();
        CS.Clicked();
        logger.DEBUG(string.Format("CS->{0}", CS.State.ToString()));
        scope.Channel = (CS.State == Selector.StateE.LEFT) ? Scope.ChannelE.A : Scope.ChannelE.B;
        scope.Update();
    }

    private void HandleBInteract()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, BSelectable.transform);
        BSelectable.AddInteractionPunch();
        B.Pressed();
        logger.DEBUG("B->PRESSED");
    }

    private void HandleBInteractEnded()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, BSelectable.transform);
        B.Released();
        logger.DEBUG("B->RELEASED");

        if (moduleState != ModuleStateE.ACTIVE)
            return;

        Signal solution;
        switch (BombInfo.GetStrikes())
        {
            case 0: solution = solutionSignalsNoStrikes.Get(inputSignal); break;
            case 1: solution = solutionSignalsOneStrike.Get(inputSignal); break;
            default: solution = solutionSignalsTwoStrikes.Get(inputSignal); break;
        }

        if (generatorSignal == solution)
        {
            logger.DEBUG(string.Format("PASS! INPUT:{0} GENERATOR:{1} SOLUTION:{2} ", inputSignal.ToString(), generatorSignal.ToString(), solution.ToString()));
            moduleState = ModuleStateE.DISARMED;
            logger.DEBUG(string.Format("STATE:{0}", moduleState.ToString()));
            BombModule.HandlePass();
        }
        else
        {
            logger.DEBUG(string.Format("STRIKE! INPUT:{0} GENERATOR:{1} SOLUTION:{2}", inputSignal.ToString(), generatorSignal.ToString(), solution.ToString()));
            BombModule.HandleStrike();
        }
    }

    private void HandleLightChange(bool on)
    {
        scope.Day = on ? true : false;
        scope.Update();
    }

    private readonly string TwitchHelpMessage =
        "!{0} channel [switch channel] | " +
        "!{0} s1 [flip S1] | " +
        "!{0} s2 [flip S2] | " +
        "!{0} s3 [flip S3] | " +
        "!{0} s1 s1 s2 s3 [flip multipple switches] | " +
        "!{0} submit [submit solution] | " +
        "Not case-senstitive";

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();

        if (command.Equals("channel"))
            return new[] { CSSelectable };
        if (command.Equals("submit"))
            return new[] { BSelectable };

        List<KMSelectable> output = new List<KMSelectable>();
        string[] commands = command.Split(' ');
        foreach (string subcommand in commands)
        {
            string subcommandTrim = subcommand.Trim();
            if (subcommandTrim.Length == 0)
                continue;

            if (subcommandTrim.Equals("s1"))
                output.Add(S1Selectable);
            else if (subcommandTrim.Equals("s2"))
                output.Add(S2Selectable);
            else if (subcommandTrim.Equals("s3"))
                output.Add(S3Selectable);
            else
                return null;
        }
        if (output.Count == 0)
            return null;

        return output.ToArray();
    }
}
