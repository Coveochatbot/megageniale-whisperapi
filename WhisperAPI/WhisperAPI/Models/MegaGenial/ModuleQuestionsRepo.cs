﻿using System;
using System.Collections.Generic;

namespace WhisperAPI.Models.MegaGenial
{
    public class ModuleQuestion : Question
    {
        private string _text;

        public override string Text => this._text;

        public ModuleQuestion(string text)
        {
            this._text = text;
            this.Id = Guid.NewGuid();
        }
    }

    public class ModuleQuestionsRepo
    {
        private static Dictionary<Module, List<string>> _questionsByModule = new Dictionary<Module, List<string>>
        {
            { Module.WireSimple, new List<string> { "Nombre de fils?", "Numéro de série?", "Couleur des fils de haut en bas?" } },
            { Module.Keypad, new List<string> { "Décris-moi les symboles?" } },
            { Module.SimonSays, new List<string> { "Numéro de série avec voyelle ou non?", "Nombre de strikes sur la bombe?", "Couleur du flash?" } },
            { Module.Password, new List<string> { "Lettres possibles pour chaque position?" } },
            { Module.WhosFirst, new List<string> { "Mots affichés?", "Étiquettes en ordre?" } },
            { Module.Maze, new List<string> { "Position des cercles?", "Position du point?", "Position du triangle?" } },
            { Module.WireSequence, new List<string> { "Combinaisons de couleur de fils, chiffre et lettre?" } },
            { Module.WireComplicated, new List<string> { "Combinaisons de couleur de fils, étoile, lumière?", "Numéro de série pair?", "Port parallèle présent?", "Nombre de piles?" } },
            { Module.Memory, new List<string> { "Quelle étape? (Jauge vert)", "Chiffre affiché en haut?", "Chiffres en ordre?" } },
            { Module.None, new List<string> { } }
        };

        public List<Question> GetQuestions(Module module)
        {
            var questions = new List<Question>();
            foreach (var entry in _questionsByModule[module])
            {
                questions.Add(new ModuleQuestion(entry));
            }

            return questions;
        }
    }
}
