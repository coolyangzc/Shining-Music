using UnityEngine;
using System.Collections;
using Leap;

public class spectrumController : MonoBehaviour {

    private const int AnimationChangeCount = 5;
    
    // Use this for initialization
	// http://docs.unity3d.com/Manual/InstantiatingPrefabs.html
	public GameObject prefab;
	public int numberOfObjects = 40;
	public float radius = 5f;
    public HandController hc;

	public GameObject[] cubes;
    private AudioClip music;
	public AudioSource source;
	public GameObject cameraPivot;

	public Material material;

	public float r = 0f;
	public float g = 0f;
	public float b = 0f;

	public float volume = 0.0f;
	public float progress = 0f;
    public string[] musicList;
    
	private float musicLength = 0f;
    private string musicDir;
    private int musicPointer;

	void Start() {
        musicDir = System.Environment.CurrentDirectory + @"\Music\";
        musicList = System.IO.Directory.GetFiles(musicDir, "*.wav");
        for (int i = 0; i < musicList.Length; ++i)
            musicList[i] = musicList[i].Replace(@"\", @"/");
        musicPointer = 0;
        PlayMusic(musicList[0]);

        hc.GetLeapController().EnableGesture(Gesture.GestureType.TYPE_SWIPE);
        hc.GetLeapController().Config.SetFloat("Gesture.Swipe.MinLength", 128.0f);
        hc.GetLeapController().Config.SetFloat("Gesture.Swipe.MinVelocity", 1000f);
        hc.GetLeapController().Config.Save();

		for (int i = 0; i < numberOfObjects; i++) {
			float angle = i * Mathf.PI * 2 / numberOfObjects;
			Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
			Instantiate(prefab, pos, Quaternion.identity);
		}
		cubes = GameObject.FindGameObjectsWithTag("cubes");
        height = new float[numberOfObjects];

		_r = r = PlayerPrefs.GetFloat("R");
		_g = g = PlayerPrefs.GetFloat("G");
		_b = b = PlayerPrefs.GetFloat("B");
		volume = PlayerPrefs.GetFloat("V");

		material.EnableKeyword ("_EMISSION");
	}

	private int counter = 0;
	private float[] spectrum, height;

    Frame currentFrame;
    private int currentFramePointer = 0;
    private double[] leftMostX = new double[8];
	private int changeColorCount = 0;
	private const int maxChangeColorCount = 300;
	private float _r,r_ = 0f;
	private float _g,g_ = 0f;
	private float _b,b_ = 0f;

	// Update is called once per frame
	void Update () {
        Animation();
		GestureRecoginize();

        // Switch the song
        if (musicLength - source.time < 0.01 && source.isPlaying)
        {
            musicPointer = (musicPointer + 1) % musicList.Length;
            source.Pause();
            PlayMusic(musicList[musicPointer]);
        }
            
	}

	private int palm2fistCnt = 0;
	private int fist2palmCnt = 0;
	private int isLastFist = 0;
    private int usedId = -1;
    private float lastGestureTimeStamp = 0f;

	void GestureRecoginize()
	{
        currentFrame = hc.GetFrame();
		GestureList gestureList = currentFrame.Gestures();
		HandList handList = currentFrame.Hands;
        currentFramePointer = (currentFramePointer + 1) & 7;
		if (handList.Count == 0) {
			isLastFist = -1;
		}
		foreach(Hand h in handList)
		{
            if (h.IsLeft) 
                continue;
            leftMostX[currentFramePointer] = h.Fingers.Leftmost.TipPosition.x;
            bool volumeControlling = true;
            Finger thumb = null, index = null;
            foreach (Finger finger in h.Fingers)
            {
                if (finger.Type == Finger.FingerType.TYPE_THUMB)
                {
                    thumb = finger;
                    if (h.PalmPosition.DistanceTo(thumb.TipPosition) < 70)
                    {
                        volumeControlling = false;
                        break;
                    }
                }
                else if (finger.Type == Finger.FingerType.TYPE_INDEX)
                {
                    index = finger;
                    if (h.PalmPosition.DistanceTo(index.TipPosition) < 70)
                    {
                        volumeControlling = false;
                        break;
                    }
                }
                else if (h.PalmPosition.DistanceTo(finger.TipPosition) > 70)
                {
                    volumeControlling = false;
                    break;
                }
            }
            if (volumeControlling)
            {
                if (thumb != null && index != null)
                {
                    volume = thumb.TipPosition.DistanceTo(index.TipPosition) / 60 - 0.6f;
                    //Debug.Log(thumb.TipPosition.DistanceTo(index.TipPosition));
                }
                lastGestureTimeStamp = currentFrame.Timestamp;
                return;
            }
			if(h.GrabStrength>=0.1)
			{
				if(h.GrabStrength<0.2)
					palm2fistCnt=0;
				else
					palm2fistCnt++;
			}
			if(h.GrabStrength<0.9)
			{
				if(h.GrabStrength>0.8)
					fist2palmCnt=0;
				else
					fist2palmCnt++;
			}
			if(h.GrabStrength>0.9)
			{
				fist2palmCnt=0;
				if (isLastFist!=1 && palm2fistCnt<100)
				{
					isLastFist = 1;
					if (source.isPlaying)
						source.Pause();
					return;
				}
				isLastFist = 1;
				break;
			}
			if(h.GrabStrength<0.1)
			{
				palm2fistCnt=0;
				if (isLastFist!=0 && fist2palmCnt<10)
				{
					isLastFist = 0;
					source.UnPause();
					return;
				}
				isLastFist = 0;
			}
            else isLastFist = -1;

            
		}
		
		foreach (Gesture g in gestureList)
		{
			if (g.Type == Gesture.GestureType.TYPE_SWIPE)
			{
                if (g.Id == usedId)
                    continue;
                //Debug.Log(currentFrame.Timestamp - lastGestureTimeStamp);
				if (g.DurationSeconds < 0.02f || currentFrame.Timestamp - lastGestureTimeStamp < 600000)
					continue;
                usedId = g.Id;
                foreach (Hand h in handList)
                    if (h.IsRight)
                        if (h.Fingers.Leftmost.TipPosition.x > leftMostX[(currentFramePointer+1)&7])
                            musicPointer += 1;
                        else
                            musicPointer -= 1;
                musicPointer = (musicPointer + musicList.Length) % musicList.Length;
                Debug.Log(musicPointer);
				PlayMusic(musicList[musicPointer]);
                lastGestureTimeStamp = currentFrame.Timestamp;
			}
		}
	}

	void OnGUI(){
		//r = GUI.HorizontalSlider (new Rect (20, 10, UnityEngine.Screen.width - 40, 20), r, 0.0F, 1.0F);
		//g = GUI.HorizontalSlider (new Rect (20, 30, UnityEngine.Screen.width - 40, 20), g, 0.0F, 1.0F);
        //b = GUI.HorizontalSlider(new Rect(20, 50, UnityEngine.Screen.width - 40, 20), b, 0.0F, 1.0F);
        volume = GUI.HorizontalSlider(new Rect(20, 10, UnityEngine.Screen.width - 40, 20), volume, 0.0F, 1.0F);
        progress = GUI.HorizontalSlider(new Rect(20, UnityEngine.Screen.height - 20, UnityEngine.Screen.width - 40, 20), progress, 0.0F, musicLength);

		PlayerPrefs.SetFloat("G",g);
		PlayerPrefs.SetFloat("B",b);
		PlayerPrefs.SetFloat("R",r);
		PlayerPrefs.SetFloat("V",volume);

		material.SetColor("_EmissionColor", new Color(r, g, b)); 
		source.volume = volume;
	}

    void PlayMusic(string fileName)
    {
        StartCoroutine(LoadSongCoroutine(fileName));
    }

    IEnumerator LoadSongCoroutine(string fileName)
    {
        string url = string.Format("file://{0}", fileName);
        WWW www = new WWW(url);
        yield return www;

        source.clip = www.GetAudioClip(false, false);
        source.Play();
        musicLength = source.clip.length;
    }

    void Animation()
    {
        // Smooth the animation
        if (counter == 0)
        {
            spectrum = AudioListener.GetSpectrumData(1024, 0, FFTWindow.Hamming);
            for (int i = 0; i < numberOfObjects; ++i)
            {
                height[i] = cubes[i].transform.localScale.y;
                spectrum[i] *= 120;
                if (spectrum[i] < 0.01f)
                    spectrum[i] = 0;
                else
                    spectrum[i] += 0.5f;
            }
        }
        if (counter < AnimationChangeCount)
            counter++;
        for (int i = 0; i < numberOfObjects; i++)
        {
            Vector3 scale = cubes[i].transform.localScale;
            scale.y = Mathf.Lerp(height[i], spectrum[i], (float)counter / AnimationChangeCount);
            cubes[i].transform.localScale = scale;
        }
        if (counter == AnimationChangeCount)
            counter = 0;

        // Alter the Shining Color
        if (changeColorCount == 0)
        {
            r_ = _r;
            g_ = _g;
            b_ = _b;
            _r = Random.Range(0f, 1f);
            _g = Random.Range(0f, 1f);
            _b = Random.Range(0f, 1f);
        }
        if (changeColorCount < maxChangeColorCount)
        {
            // Smooth the alteration of the Color;
            changeColorCount++;

            r = Mathf.Lerp(r_, _r, (float)changeColorCount / maxChangeColorCount);
            g = Mathf.Lerp(g_, _g, (float)changeColorCount / maxChangeColorCount);
            b = Mathf.Lerp(b_, _b, (float)changeColorCount / maxChangeColorCount);

        }
        else
        {
            changeColorCount = 0;
        }
        progress = source.time;
        cameraPivot.transform.Rotate(0, 0.1F, 0);
    }
}