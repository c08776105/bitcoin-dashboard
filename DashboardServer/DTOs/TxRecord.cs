namespace DashboardServer.DTOs;

public class TxRecord(
    string txId,
    uint version,
    uint txInCount,
    List<TxInput> txInputs,
    uint txOutCount,
    List<TxOutput> txOutputs,
    List<string> txWitness,
    uint lockTime)
{
    public string TxId { get; set; } = txId;
    public uint Version { get; set; } = version;
    public uint TxInCount { get; set; } = txInCount;
    public List<TxInput> TxInputs { get; set; } = txInputs;
    public uint TxOutCount { get; set; } = txOutCount;
    public List<TxOutput> TxOutputs { get; set; } = txOutputs;
    public List<string> TxWitness { get; set; } = txWitness;
    public uint LockTime { get; set; } = lockTime;
}