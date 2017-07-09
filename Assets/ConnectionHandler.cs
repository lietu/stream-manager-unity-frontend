using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

[Serializable]
public class BitsMessage
{
    public string type;
    public string name;
    public int bits;
    public string message;
}

[Serializable]
public class BitsAction
{
    public string prefix;
}

[Serializable]
public class BitsActions
{
    public string type;
    public BitsAction[] actions;
}

public class BitData
{
    public int amount;
    public string style;

    public BitData(int amount, string style = "bit")
    {
        this.amount = amount;
        this.style = style;
    }

    static public BitData Random()
    {
        int amount = 1;
        float rand = UnityEngine.Random.Range(1F, 100F);

        if (rand > 95F)
        {
            amount = 1000;
        } else if (rand > 90F)
        {
            amount = 100;
        }

        return new BitData(amount);
    }
}

public class ConnectionHandler : MonoBehaviour {
    public string streamManager = "ws://192.168.1.11:60606/events";
    public float createRate = 0.1F;
    public bool test = false;
    public GameObject fireball;

    private WebSocket socket;
    private float nextCreate = 0;
    private List<GameObject> objects;
    private List<BitData> queue;

    private List<string> cheerPrefixes;

    // Use this for initialization
    void Start()
    {
        this.objects = new List<GameObject>();
        this.queue = new List<BitData>();

        this.createSocket();
	}

    // Update is called once per frame
    void Update () {
        if (this.test)
        {
            if (Time.time > this.nextCreate)
            {
                this.queue.Add(BitData.Random());
                this.nextCreate = Time.time + this.createRate;
            }
        }

        if (this.queue.Count > 0)
        {
            BitData next = this.queue[0];
            //Debug.Log(this.queue.Count.ToString() + " bits waiting in queue, next " + next.amount.ToString());
            this.queue.RemoveAt(0);
            this.Create(next);
        }

        this.objects.ForEach(delegate (GameObject ball) {
            if (ball.transform.position.y < -100F)
            {
                //Debug.Log("Removing object outside of bounds", ball);
                this.objects.Remove(ball);
                GameObject.Destroy(ball);
            }
        });
	}

    void Create(BitData bit) {
        var sphere = this.GetBit(bit);
        sphere.transform.position = this.RandomPosition();
        sphere.transform.rotation = this.RandomRotation();

        if (bit.amount == 666)
        {
            this.MakeFireball(sphere);
        } 
        else
        {
            // this.addTexture(sphere, bit);
            this.SetStyleMaterial(sphere, bit);

            sphere.transform.localScale = this.GetScale(bit);


            var rb = sphere.AddComponent<Rigidbody>();
            rb.mass = this.GetMass(bit);
            rb.velocity = this.GetVelocity(bit);
            //Debug.Log("Mass " + rb.mass.ToString());
            //Debug.Log("Position " + sphere.transform.position.ToString());
            //Debug.Log("Velocity for " + bit.amount.ToString() + " bits " + rb.velocity.ToString());
        }

        this.objects.Add(sphere);
    }

    Vector3 GetVelocity(BitData bit)
    {
        float maxY = bit.amount;

        if (bit.amount >= 1000)
        {
            maxY *= 4;
        }
        else if (bit.amount >= 100)
        {
            maxY *= 2;
        }

        if (maxY > 50F)
        {
            maxY = 50.0F;
        }

        float x = UnityEngine.Random.Range(-0.1F, 0.1F);
        float y = UnityEngine.Random.Range(-1, maxY*-1);
        float z = UnityEngine.Random.Range(-0.1F, 0.1F);
        return new Vector3(x, y, z); 
    }

    GameObject GetBit(BitData bit)
    {
        if (bit.amount == 666)
        {
            return Instantiate(this.fireball);
        }

        return GameObject.CreatePrimitive(PrimitiveType.Sphere);
    }

    void SetStyleMaterial(GameObject sphere, BitData bit)
    {
        var renderer = sphere.GetComponent<Renderer>();
        renderer.material = this.GetBitMaterial(bit);
    }

    void MakeFireball(GameObject sphere)
    {
        var renderer = sphere.GetComponent<Renderer>();
        renderer.material = this.GetFireballMaterial();

        var rb = sphere.AddComponent<Rigidbody>();
        rb.mass = 1000000000000;

        float x = UnityEngine.Random.Range(-0.1F, 0.1F);
        float y = UnityEngine.Random.Range(-50F, -75F);
        float z = UnityEngine.Random.Range(-0.1F, 0.1F);
        rb.velocity = new Vector3(x, y, z);
    }

    Material GetBitMaterial(BitData bit)
    {
        return Resources.Load(bit.style, typeof(Material)) as Material;
    }


    Material GetFireballMaterial()
    {
        return Resources.Load("Fireball", typeof(Material)) as Material;
    }

    Vector3 GetScale(BitData bit)
    {
        float scale = 0.7F + (0.01F * bit.amount);
        if (scale > 1.0F)
        {
            scale = 1.0F;
        }

        return new Vector3(scale, scale, scale);
    }

    int GetMass(BitData bit)
    {
        var mass = bit.amount * 10;

        if (bit.amount >= 100)
        {
            mass *= 10;
        }

        return mass;
    }

    Vector3 RandomPosition()
    {
        float x = UnityEngine.Random.Range(-0.5F, 0.5F);
        float y = UnityEngine.Random.Range(20, 30);
        float z = UnityEngine.Random.Range(-0.5F, 0.5F);
        return new Vector3(x, y, z);
    }

    Quaternion RandomRotation()
    {
        float x = UnityEngine.Random.Range(-0.1F, 0.1F);
        float y = UnityEngine.Random.Range(-0.1F, 0.1F);
        float z = UnityEngine.Random.Range(-0.1F, 0.1F);
        float w = UnityEngine.Random.Range(-0.1F, 0.1F);
        return new Quaternion(x, y, z, w);
    }

    /*
     * WebSocket handling
     */

    void createSocket()
    {
        if (this.socket != null)
        {
            return;
        }

        Debug.Log("Creating socket");

        this.reconnect();
    }

    void reconnect()
    {
        Debug.Log("Connecting to socket at " + this.streamManager);
        this.socket = new WebSocket(this.streamManager);
        this.socket.OnOpen += new EventHandler(this.onSocketOpen);
        this.socket.OnClose += new EventHandler<WebSocketSharp.CloseEventArgs>(this.onSocketClosed);
        this.socket.OnMessage += new EventHandler<WebSocketSharp.MessageEventArgs>(this.onSocketMessage);
        this.socket.Connect();
    }

    void onSocketOpen(object sender, EventArgs e)
    {
        Debug.Log("Connected to socket at " + this.streamManager);
    }

    void onSocketClosed(object sender, CloseEventArgs e)
    {
        Debug.Log("Socket closed");
        if (this.socket != null)
        {
            this.reconnect();
        }
    }

    void onSocketMessage(object sender, MessageEventArgs e)
    {
        //Debug.Log(e.Data);
        BitsMessage msg = JsonUtility.FromJson<BitsMessage>(e.Data);
        if (msg.type == "bits")
        {
            var words = Regex.Replace(msg.message, @"\s+", " ").Split(' ');
            foreach (var word in words)
            {
                string wordl = word.ToLower();
                foreach (var prefix in this.cheerPrefixes)
                {
                    if (wordl.StartsWith(prefix))
                    {
                        var bits = Int32.Parse(wordl.Remove(0, prefix.Length));
                        this.queue.Add(new BitData(bits));
                    }
                }
            }
        }
        else if (msg.type == "bits_actions")
        {
            var actions = JsonUtility.FromJson<BitsActions>(e.Data);
            this.cheerPrefixes = new List<string>();
            foreach (var action in actions.actions)
            {
                Debug.Log("Adding cheer prefix " + action.prefix);
                this.cheerPrefixes.Add(action.prefix.ToLower());
            }
        }
        else if (msg.type == "follower")
        {
            var bits = 1;
            this.queue.Add(new BitData(bits, "follower"));
        }
        else if (msg.type == "host")
        {
            var bits = 1;
            this.queue.Add(new BitData(bits, "host"));
        }
        else if (msg.type == "subscriber")
        {
            var bits = 666;
            this.queue.Add(new BitData(bits));
        }
        else
        {
            Debug.Log("Ignored event: " + e.Data);
        }
    }
}
