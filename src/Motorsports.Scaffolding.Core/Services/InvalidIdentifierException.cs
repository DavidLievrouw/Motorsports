using System;

namespace Motorsports.Scaffolding.Core.Services {
  public class InvalidIdentifierException : ArgumentException {
    public InvalidIdentifierException() {}
    public InvalidIdentifierException(string message) : base(message) {}
    public InvalidIdentifierException(string message, Exception innerException) : base(message, innerException) {}
    public InvalidIdentifierException(string message, string paramName, Exception innerException) : base(message, paramName, innerException) {}
    public InvalidIdentifierException(string message, string paramName) : base(message, paramName) {}
  }
}