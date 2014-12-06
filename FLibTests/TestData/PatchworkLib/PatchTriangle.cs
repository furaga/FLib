using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchworkLib.PatchMesh
{
    public struct PatchTriangle
    {
        int _idx0;
        int _idx1;
        int _idx2;
        string _key;

        public int Idx0 { get { return _idx0; } }
        public int Idx1 { get { return _idx1; } }
        public int Idx2 { get { return _idx2; } }
        public string TextureKey { get { return _key; } } 

        public PatchTriangle(int idx0, int idx1, int idx2, string textureKey)
        {
            this._idx0 = idx0;
            this._idx1 = idx1;
            this._idx2 = idx2;
            this._key = textureKey;
        }

        public PatchTriangle(PatchTriangle t)
        {
            this._idx0 = t._idx0;
            this._idx1 = t._idx1;
            this._idx2 = t._idx2;
            this._key = t._key;
        }

    }
}