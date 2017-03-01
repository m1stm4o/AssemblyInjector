using DLLInjection;
using UnityEngine;

public class TestScript : MonoBehaviour {

    [RequireToString]
    class C0 {

    }

    [RequireToString]
    class C1 {
        int n;
    }


    [RequireToString]
    class C2 {
        string s;
        double d;
    }

    [RequireToString]
    class C3 {
        System.IntPtr np;
        char c = 'a';
        C0 c0;
        public override string ToString() {
            return string.Format("[np={0}, c={1}, c0={2}]", this.np, this.c, this.c0);
        }
    }

    [RequireToString]
    class C4 {
        ST st;
        public E e;

        C3 c3;

        int[] ns;
    }
    [RequireToString]
    class C5 {
        ST st;
        public E e;

        C3 c3;

        int[] ns;

        string s;
    }



    class C6 {

        ST st;
        public E e;

        C3 c3;

        int[] ns;

        string s;
        System.IntPtr np;

        public override string ToString() {


            return string.Format("[st={0}, e={1}, c3={2}, ns={3}, s={4}, np={5}]", st, e, c3, ns, s, np);
        }
    }
    [RequireToString]
    class C62 {

        ST st;
        public E e;

        C3 c3;

        int[] ns;

        string s;
        System.IntPtr np;

        public override string ToString() {


            return string.Format("[st={0}, e={1}, c3={2}, ns={3}, s={4}, np={5}]", st, e, c3, ns, s, np);
        }
    }

    [RequireToString]
    class C61 {

        ST st;
        public E e;

        C3 c3;

        int[] ns;

        string s;
        System.IntPtr np;
    }
    [RequireToString]
    struct ST6 {

        ST st;
        public E e;

        C3 c3;

        int[] ns;

        string s;
        System.IntPtr np;
    }
    struct ST61 {

        ST st;
        public E e;

        C3 c3;

        int[] ns;

        string s;
        System.IntPtr np;
    }
    [RequireToString]
    struct ST62 {

        ST st;
        public E e;

        C3 c3;

        int[] ns;

        string s;
        System.IntPtr np;
        public override string ToString() {


            return string.Format("[st={0}, e={1}, c3={2}, ns={3}, s={4}, np={5}]", st, e, c3, ns, s, np);
        }
    }

    [RequireToString]
    static class SC {

    }


    // Use this for initialization
    void Start() {
        Test();
        TestStr();
        TestD();
        TestParI(30);
        TestPar5(4, null, 30.6, E.EE, new ST(98));

        TestScript t = null;

        E e = E.EE;
        double d;
        double d2 = 343.3;
        ST st = new ST(93);
        Debug.Log(TestParrefout(4, this, out d, out t, ref d2, ref e, ref st));

        Debug.Log(new C0());
        Debug.Log(new C1());
        Debug.Log(new C2());
        Debug.Log(new C3());
        Debug.Log(new C4());
        Debug.Log(new C5());
        Debug.Log(new C6());
        Debug.Log(new C61());
        Debug.Log(new C62());
    }

    // Update is called once per frame
    void Update() {

    }

    [InsertLog]
    void Test() {
        Debug.Log("Tst");

        int nnn = 5;
        double d = 2;

    }

    [InsertLog]
    static string TestStr() {

        TestScript s;

        float f = 2f;

        Debug.Log("Exit TestScript.TestStr, Return Value " + null);
        return "hehe";
    }

    [InsertLog]
    double TestD() {

        TestScript s;

        float f = 2f;

        Debug.Log("Exit TestScript.TestStr, Return Value " + null);
        return 85.34;
    }

    [InsertLog]
    int TestParI(int n) {

        return n + 5;
    }
    enum E {
        EE,
        EE2
    }
    struct ST {
        public int n;
        public ST(int n) { this.n = n; }
        public override string ToString() {
            return "[ST:n=" + n + "]";
        }
    }
    [InsertLog]
    int TestPar5(int n, TestScript t, double d, E e, ST st) {
        Debug.Log(string.Format("P5 |{0}||", t));
        return n + 5;
    }
    [InsertLog]
    double TestParrefout(int n, TestScript t1, out double d1, out TestScript t, ref double d, ref E e, ref ST st) {
        Debug.Log(string.Format("sd{0}{1}{2}{3}", n, t1, d, e, st));
        e = E.EE2;
        d1 = 34.234;
        t = null;

        return d;
    }
    [InsertLog]
    int TestPar9(int n1, int n2, int n3, int n4, int n5, int n6, int n7, int n8, int n9) {

        return n1 + n2;
    }
}
