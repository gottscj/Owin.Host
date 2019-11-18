using System;
using System.Text;

namespace Gottscj.Owin.Host.SocketHttpListener
{
  /// <summary>
  /// Contains the event data associated with a <see cref="WebSocket.OnMessage"/> event.
  /// </summary>
  /// <remarks>
  /// A <see cref="WebSocket.OnMessage"/> event occurs when the <see cref="WebSocket"/> receives
  /// a text or binary data frame.
  /// If you want to get the received data, you access the <see cref="MessageEventArgs.Data"/> or
  /// <see cref="MessageEventArgs.RawData"/> property.
  /// </remarks>
  public class MessageEventArgs : EventArgs
  {
    #region Private Fields

    private readonly string _data;
    private readonly Opcode _opcode;
    private readonly byte[] _rawData;

    #endregion

    #region Internal Constructors

    internal MessageEventArgs (Opcode opcode, byte[] data)
    {
      _opcode = opcode;
      _rawData = data;
      _data = ConvertToString (opcode, data);
    }

    internal MessageEventArgs (Opcode opcode, PayloadData payload)
    {
      _opcode = opcode;
      _rawData = payload.ApplicationData;
      _data = ConvertToString (opcode, _rawData);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the received data as a <see cref="string"/>.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that contains the received data.
    /// </value>
    public string Data => _data;

    /// <summary>
    /// Gets the received data as an array of <see cref="byte"/>.
    /// </summary>
    /// <value>
    /// An array of <see cref="byte"/> that contains the received data.
    /// </value>
    public byte [] RawData => _rawData;

    /// <summary>
    /// Gets the type of the received data.
    /// </summary>
    /// <value>
    /// One of the <see cref="Opcode"/> values, indicates the type of the received data.
    /// </value>
    public Opcode Type => _opcode;

    #endregion

    #region Private Methods

    private static string ConvertToString (Opcode opcode, byte [] data)
    {
      return data.Length == 0
             ? String.Empty
             : opcode == Opcode.Text
               ? Encoding.UTF8.GetString (data, 0, data.Length)
               : opcode.ToString ();
    }

    #endregion
  }
}
