using AbilityKit.AI.Abstractions;

namespace AbilityKit.AI.Inference;

/// <summary>
/// 描述 AI 策略模型使用的一个输入或输出张量。
/// </summary>
public readonly struct AiModelTensorSpec
{
    /// <summary>
    /// 创建包含稳定张量名称、元素长度和值类型的张量规格。
    /// </summary>
    public AiModelTensorSpec(string name, int length, AiSpaceValueType valueType = AiSpaceValueType.Float32)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "tensor" : name;
        Length = length < 0 ? 0 : length;
        ValueType = valueType;
    }

    /// <summary>
    /// 获取模型张量名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取扁平张量的元素数量。
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// 获取张量值类型。
    /// </summary>
    public AiSpaceValueType ValueType { get; }
}

/// <summary>
/// 描述 AI 模型如何把环境观测映射到动作缓冲。
/// </summary>
public readonly struct AiModelPolicySpec
{
    /// <summary>
    /// 根据环境规格和张量元数据创建模型策略规格。
    /// </summary>
    public AiModelPolicySpec(
        AiObservationSpec observationSpec,
        AiActionSpec actionSpec,
        AiModelTensorSpec inputTensor,
        AiModelTensorSpec continuousOutputTensor,
        AiModelTensorSpec discreteOutputTensor)
    {
        ObservationSpec = observationSpec;
        ActionSpec = actionSpec;
        InputTensor = inputTensor;
        ContinuousOutputTensor = continuousOutputTensor;
        DiscreteOutputTensor = discreteOutputTensor;
    }

    /// <summary>
    /// 获取模型期望的观测规格。
    /// </summary>
    public AiObservationSpec ObservationSpec { get; }

    /// <summary>
    /// 获取模型产出的动作规格。
    /// </summary>
    public AiActionSpec ActionSpec { get; }

    /// <summary>
    /// 获取接收观测值的输入张量规格。
    /// </summary>
    public AiModelTensorSpec InputTensor { get; }

    /// <summary>
    /// 获取产出连续动作的输出张量规格。
    /// </summary>
    public AiModelTensorSpec ContinuousOutputTensor { get; }

    /// <summary>
    /// 获取产出离散动作的输出张量规格。
    /// </summary>
    public AiModelTensorSpec DiscreteOutputTensor { get; }

    /// <summary>
    /// 直接根据 AI 环境创建模型策略规格。
    /// </summary>
    public static AiModelPolicySpec FromEnvironment(
        IAiEnvironment environment,
        string inputTensorName = "observation",
        string continuousOutputTensorName = "continuous_action",
        string discreteOutputTensorName = "discrete_action")
    {
        if (environment == null) throw new ArgumentNullException(nameof(environment));

        return new AiModelPolicySpec(
            environment.ObservationSpec,
            environment.ActionSpec,
            new AiModelTensorSpec(inputTensorName, environment.ObservationSpec.Length, environment.ObservationSpec.ValueType),
            new AiModelTensorSpec(continuousOutputTensorName, environment.ActionSpec.ContinuousLength),
            new AiModelTensorSpec(discreteOutputTensorName, environment.ActionSpec.DiscreteLength, AiSpaceValueType.Int32));
    }
}

/// <summary>
/// 表示一次由观测缓冲构建的模型执行输入。
/// </summary>
public sealed class AiModelInput
{
    /// <summary>
    /// 创建模型输入，并按策略规格校验观测长度。
    /// </summary>
    public AiModelInput(AiModelPolicySpec spec, ReadOnlyMemory<float> observation)
    {
        if (observation.Length != spec.InputTensor.Length)
        {
            throw new ArgumentException("AI model observation length does not match the model input tensor spec.", nameof(observation));
        }

        Spec = spec;
        Observation = observation;
    }

    /// <summary>
    /// 获取本次执行使用的策略规格。
    /// </summary>
    public AiModelPolicySpec Spec { get; }

    /// <summary>
    /// 获取传递给模型执行器的扁平观测值。
    /// </summary>
    public ReadOnlyMemory<float> Observation { get; }
}

/// <summary>
/// 表示一次可复制到 AI 动作缓冲的模型执行输出。
/// </summary>
public sealed class AiModelOutput
{
    /// <summary>
    /// 根据连续动作数组和离散动作数组创建模型输出。
    /// </summary>
    public AiModelOutput(float[] continuousAction, int[] discreteAction)
    {
        ContinuousAction = continuousAction ?? throw new ArgumentNullException(nameof(continuousAction));
        DiscreteAction = discreteAction ?? throw new ArgumentNullException(nameof(discreteAction));
    }

    /// <summary>
    /// 获取模型产出的连续动作值。
    /// </summary>
    public float[] ContinuousAction { get; }

    /// <summary>
    /// 获取模型产出的离散动作值。
    /// </summary>
    public int[] DiscreteAction { get; }
}

/// <summary>
/// 在稳定策略适配器后执行已训练的 AI 模型。
/// </summary>
public interface IAiModelExecutor : IDisposable
{
    /// <summary>
    /// 获取此执行器支持的模型策略规格。
    /// </summary>
    AiModelPolicySpec Spec { get; }

    /// <summary>
    /// 对一次观测输入执行推理。
    /// </summary>
    AiModelOutput Run(AiModelInput input);
}

/// <summary>
/// 将 AI 模型执行器适配到运行时 <see cref="IAiPolicy"/> 契约。
/// </summary>
public sealed class AiModelPolicy : IAiPolicy
{
    private readonly IAiModelExecutor _executor;

    /// <summary>
    /// 创建一个把决策委托给模型执行器的策略。
    /// </summary>
    public AiModelPolicy(IAiModelExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        ActionSpec = executor.Spec.ActionSpec;
    }

    /// <inheritdoc />
    public AiActionSpec ActionSpec { get; }

    /// <inheritdoc />
    public void Decide(in AiObservationBuffer observation, AiActionBuffer action)
    {
        if (observation.Length != _executor.Spec.ObservationSpec.Length)
        {
            throw new InvalidOperationException("AI policy observation length does not match the model policy spec.");
        }

        if (action.Spec.ContinuousLength != ActionSpec.ContinuousLength ||
            action.Spec.DiscreteLength != ActionSpec.DiscreteLength)
        {
            throw new InvalidOperationException("AI policy action buffer does not match the model policy spec.");
        }

        var output = _executor.Run(new AiModelInput(_executor.Spec, observation.Values));
        if (output.ContinuousAction.Length != ActionSpec.ContinuousLength ||
            output.DiscreteAction.Length != ActionSpec.DiscreteLength)
        {
            throw new InvalidOperationException("AI model output length does not match the policy action spec.");
        }

        Array.Copy(output.ContinuousAction, action.Continuous, action.Continuous.Length);
        Array.Copy(output.DiscreteAction, action.Discrete, action.Discrete.Length);
    }
}

/// <summary>
/// 用于测试和适配的执行器，把模型执行委托给外部传入的函数。
/// </summary>
public sealed class DelegateAiModelExecutor : IAiModelExecutor
{
    private readonly Func<AiModelInput, AiModelOutput> _run;

    /// <summary>
    /// 创建由委托函数驱动的执行器。
    /// </summary>
    public DelegateAiModelExecutor(AiModelPolicySpec spec, Func<AiModelInput, AiModelOutput> run)
    {
        Spec = spec;
        _run = run ?? throw new ArgumentNullException(nameof(run));
    }

    /// <inheritdoc />
    public AiModelPolicySpec Spec { get; }

    /// <inheritdoc />
    public AiModelOutput Run(AiModelInput input)
    {
        if (input.Spec.ActionSpec.ContinuousLength != Spec.ActionSpec.ContinuousLength ||
            input.Spec.ActionSpec.DiscreteLength != Spec.ActionSpec.DiscreteLength ||
            input.Spec.ObservationSpec.Length != Spec.ObservationSpec.Length)
        {
            throw new ArgumentException("AI model input spec does not match this executor.", nameof(input));
        }

        return _run(input);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
