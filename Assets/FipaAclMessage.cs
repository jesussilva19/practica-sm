using System;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class FipaAclMessage
{
    public string Performative { get; set; }  // Acto comunicativo (INFORM, REQUEST, etc.)
    public string Sender { get; set; } // Agente remitente
    public List<string> Receivers { get; set; } = new List<string>(); // Agentes destinatarios
    public string Content { get; set; }  // Contenido del mensaje
    public string Language { get; set; }    // Lenguaje del contenido
    public string Ontology { get; set; }    // Ontología utilizada
    public string ConversationId { get; set; }
}