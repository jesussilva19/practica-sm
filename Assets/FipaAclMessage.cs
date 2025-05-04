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
    public string ConversationId { get; set; } // Identificador de conversación
}

public static class FipaPerformatives
{
    public const string INFORM = "INFORM";
    public const string REQUEST = "REQUEST";
    public const string AGREE = "AGREE";
    public const string REFUSE = "REFUSE";
    public const string CONFIRM = "CONFIRM";
    public const string DISCONFIRM = "DISCONFIRM";
    public const string CFP = "CFP";
    public const string PROPOSE = "PROPOSE";
    public const string ACCEPT_PROPOSAL = "ACCEPT_PROPOSAL";
    public const string REJECT_PROPOSAL = "REJECT_PROPOSAL";
    public const string QUERY_IF = "QUERY_IF";
    public const string QUERY_REF = "QUERY_REF";
    public const string SUBSCRIBE = "SUBSCRIBE";
    public const string FAILURE = "FAILURE";
    public const string NOT_UNDERSTOOD = "NOT_UNDERSTOOD";
}