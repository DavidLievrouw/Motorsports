using System;

namespace Build.Tasks {
  public abstract class TaskProps {
    protected TaskProps(GlobalProps globalProps) {
      GlobalProps = globalProps ?? throw new ArgumentNullException(nameof(globalProps));
    }

    public GlobalProps GlobalProps { get; }
  }
}