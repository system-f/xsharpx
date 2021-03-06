using System;
using System.Collections;
using System.Collections.Generic;

namespace XSharpx {
  public struct NonEmptyList<A> : IEnumerable<A> {
    private readonly A head;
    private readonly List<A> tail;

    internal NonEmptyList(A head, List<A> tail) {
      this.head = head;
      this.tail = tail;
    }

    public Store<A, NonEmptyList<A>> Head {
      get {
        var t = this;
        return head.StoreSet(h => h & t.tail);
      }
    }

    public Store<List<A>, NonEmptyList<A>> Tail {
      get {
        var t = this;
        return tail.StoreSet(tl => t.head & tl);
      }
    }

    public List<A> List => head + tail;

    public NonEmptyList<B> Select<B>(Func<A, B> f) =>
      new NonEmptyList<B>(f(head), tail.Select(f));

    public NonEmptyList<B> SelectMany<B>(Func<A, NonEmptyList<B>> f) =>
      f(head).Append(tail.SelectMany(a => f(a).List));

    public NonEmptyList<C> SelectMany<B, C>(Func<A, NonEmptyList<B>> p, Func<A, B, C> f) =>
      SelectMany<C>(a => p(a).Select<C>(b => f(a, b)));

    public NonEmptyList<NonEmptyList<A>> Duplicate => Extend(q => q);

    public NonEmptyList<B> Extend<B>(Func<NonEmptyList<A>, B> f) {
      var b = ListBuffer<B>.Empty();
      var a = tail;

      while(!a.IsEmpty) {
        b.Snoc(f(new NonEmptyList<A>(a.UnsafeHead, a.UnsafeTail)));
        a = a.UnsafeTail;
      }

      return new NonEmptyList<B>(f(this), b.ToList);
    }

    public NonEmptyList<C> ProductWith<B, C>(NonEmptyList<B> o, Func<A, Func<B, C>> f) =>
      SelectMany<C>(a => o.Select<C>(b => f(a)(b)));
    
    public NonEmptyList<Pair<A, B>> Product<B>(NonEmptyList<B> o) =>
      ZipWith<B, Pair<A, B>>(o, Pair<A, B>.pairF());

    public NonEmptyList<A> Append(List<A> x) =>
      new NonEmptyList<A>(head, tail.Append(x));

    public NonEmptyList<A> Append(NonEmptyList<A> x) => Append(x.List);

    public static NonEmptyList<A> operator +(List<A> s, NonEmptyList<A> t) => s.Append(t);

    public static NonEmptyList<A> operator &(A h, NonEmptyList<A> t) => new NonEmptyList<A>(h, t.List);

    public static NonEmptyList<A> operator *(NonEmptyList<A> a1, NonEmptyList<A> a2) => a1.Append(a2);

    public static NonEmptyList<A> nel(A h, params A[] a) => new NonEmptyList<A>(h, List<A>.list(a));

    IEnumerator<A> IEnumerable<A>.GetEnumerator() => ((IEnumerable<A>)List).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)List).GetEnumerator();

    public X Uncons<X>(Func<A, List<A>, X> headTail) => headTail(head, tail);

    public B FoldRight<B>(Func<A, B, B> f, B b) => List.FoldRight(f, b);

    public B FoldLeft<B>(Func<B, A, B> f, B b) => List.FoldLeft (f, b);

    public A SumRight(Semigroup<A> m) => tail.FoldRight<A>(m.Op, head);

    public A SumLeft(Semigroup<A> m) => tail.FoldLeft<A>(m.Op, head);

    public B SumMapRight<B>(Func<A, B> f, Semigroup<B> m) =>
      tail.FoldRight<B>((a, b) => m.Op(f(a), b), f(head));
    

    public B SumMapLeft<B>(Func<A, B> f, Semigroup<B> m) =>
      tail.FoldLeft<B>((a, b) => m.Op(a, f(b)), f(head));

    public void ForEach(Action<A> a) {
      foreach(A x in this) {
        a(x);
      }
    }

    public List<A> Where(Func<A, bool> f) => List.Where(f);

    public List<A> Take(int n) => List.Take(n);

    public List<A> Drop(int n) => List.Drop(n);

    public List<A> TakeWhile(Func<A, bool> p) => List.TakeWhile(p);


    public List<A> DropWhile(Func<A, bool> p) => List.DropWhile(p);
    public int Length => 1 + tail.Length;

    public NonEmptyList<A> Reverse => FoldLeft<NonEmptyList<A>>((b, a) => a & b, new NonEmptyList<A>(head, List<A>.Empty));

    public Option<A> this [int n] => n == 0 ? head.Some() : tail[n - 1];

    public NonEmptyList<C> ZipWith<B, C>(NonEmptyList<B> bs, Func<A, Func<B, C>> f) =>
      new NonEmptyList<C>(f(head)(bs.head), tail.ZipWith(bs.tail, f));

    public NonEmptyList<Pair<A, B>> Zip<B>(NonEmptyList<B> bs) => ZipWith<B, Pair<A, B>>(bs, a => b => a.And(b));

    public bool All(Func<A, bool> f) => f(head) && tail.All(f);

    public bool Any(Func<A, bool> f) => f(head) || tail.Any(f);

    public List<NonEmptyList<B>> TraverseList<B>(Func<A, List<B>> f) =>
      f(head).ProductWith<List<B>, NonEmptyList<B>>(tail.TraverseList(f), h => t => h & t);

    public Option<NonEmptyList<B>> TraverseOption<B>(Func<A, Option<B>> f) =>
      f(head).ZipWith<List<B>, NonEmptyList<B>>(tail.TraverseOption(f), h => t => h & t);

    public Terminal<NonEmptyList<B>> TraverseTerminal<B>(Func<A, Terminal<B>> f) =>
      f(head).ZipWith<List<B>, NonEmptyList<B>>(tail.TraverseTerminal(f), h => t => h & t);

    public Input<NonEmptyList<B>> TraverseInput<B>(Func<A, Input<B>> f) =>
      f(head).ZipWith<List<B>, NonEmptyList<B>>(tail.TraverseInput(f), h => t => h & t);

    public Either<X, NonEmptyList<B>> TraverseEither<X, B>(Func<A, Either<X, B>> f) =>
      f(head).ZipWith<List<B>, NonEmptyList<B>>(tail.TraverseEither(f), h => t => h & t);

    public NonEmptyList<NonEmptyList<B>> TraverseNonEmptyList<B>(Func<A, NonEmptyList<B>> f) =>
      f(head).ProductWith<List<B>, NonEmptyList<B>>(tail.TraverseNonEmptyList(f), h => t => h & t);

    public Pair<X, NonEmptyList<B>> TraversePair<X, B>(Func<A, Pair<X, B>> f, Monoid<X> m) =>
      f(head).Constrain(m).ZipWith<List<B>, NonEmptyList<B>>(tail.TraversePair(f, m).Constrain(m), h => t => h & t).Pair;

    public Func<X, NonEmptyList<B>> TraverseFunc<X, B>(Func<A, Func<X, B>> f) =>
      f(head).ZipWith<B, List<B>, NonEmptyList<B>, X>(tail.TraverseFunc(f), h => t => h & t);

    public Tree<NonEmptyList<B>> TraverseTree<B>(Func<A, Tree<B>> f) =>
      f(head).ZipWith<List<B>, NonEmptyList<B>>(tail.TraverseTree(f), h => t => h & t);

    public ListZipper<A> Zipper => new ListZipper<A>(List<A>.Empty, head, tail);

  }

  public static class NonEmptyListExtension {
    public static NonEmptyList<B> Select<A, B>(this NonEmptyList<A> ps, Func<A, B> f) =>
      new NonEmptyList<B>(f(ps.Head.Get), ps.Tail.Get.Select(f));

    public static NonEmptyList<B> SelectMany<A, B>(this NonEmptyList<A> ps, Func<A, NonEmptyList<B>> f) =>
      f(ps.Head.Get).Append(ps.Tail.Get.SelectMany(a => f(a).List));

    public static NonEmptyList<C> SelectMany<A, B, C>(this NonEmptyList<A> ps, Func<A, NonEmptyList<B>> p, Func<A, B, C> f) =>
      SelectMany(ps, a => Select(p(a), b => f(a, b)));

    public static NonEmptyList<B> Apply<A, B>(this NonEmptyList<Func<A, B>> f, NonEmptyList<A> o) =>
      f.ProductWith<A, B>(o, a => b => a(b));

    public static NonEmptyList<B> ApplyZip<A, B>(this NonEmptyList<Func<A, B>> f, NonEmptyList<A> o) =>
      f.ZipWith<A, B>(o, a => b => a(b));

    public static NonEmptyList<A> Flatten<A>(this NonEmptyList<NonEmptyList<A>> o) =>
      o.SelectMany(z => z);

    public static Pair<NonEmptyList<A>, NonEmptyList<B>> Unzip<A, B>(this NonEmptyList<Pair<A, B>> p) =>
      p.Tail.Get.Unzip().BinarySelect(ta => p.Head.Get._1.Get & ta, tb => p.Head.Get._2.Get & tb);

    public static NonEmptyList<A> NonEmptyListValue<A>(this A a) => new NonEmptyList<A>(a, List<A>.Empty);

    public static NonEmptyList<B> UnfoldNonEmptyList<A, B>(this A a, Func<A, Pair<B, A>> f, Func<A, Option<Pair<B, A>>> g) {
      var aa = f(a);
      return aa._1.Get & aa._2.Get.UnfoldList(g);
    }
  }
}
