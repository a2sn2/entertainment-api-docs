namespace FoundationKit.Application.Messaging;

public interface ICommand { }

public interface ICommand<out TResponse> { }
