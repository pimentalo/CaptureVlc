using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaptureVlc
{
    /// <summary>
    /// Format vidéo pour affichage dans la liste déroulante
    /// </summary>
    public class FormatVideo
    {
        public FormatVideo(string nom, string optionsVlc)
        {
            Nom = nom; OptionsVlc = optionsVlc;
        }
        public string Nom { get; set; }
        public string OptionsVlc { get; set; }
    }
}
