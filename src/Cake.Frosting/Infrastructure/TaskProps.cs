using System;

namespace Build.Infrastructure {
  public abstract class TaskProps {
    protected TaskProps(GlobalProps globalProps) {
      GlobalProps = globalProps ?? throw new ArgumentNullException(nameof(globalProps));
    }

    public GlobalProps GlobalProps { get; }
  }
}