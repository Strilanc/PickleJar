namespace Strilanc.PickleJar {
    public interface IPacker<in T> {
        byte[] Pack(T value);
    }
}